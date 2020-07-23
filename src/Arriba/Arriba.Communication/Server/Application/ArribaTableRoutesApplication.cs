// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Security.Principal;
using System.Threading.Tasks;

using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Expressions;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Monitoring;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Arriba.Types;

namespace Arriba.Server.Application
{
    [Export(typeof(IRoutedApplication))]
    internal class ArribaTableRoutesApplication : ArribaApplication, IArribaManagementService
    {
        private readonly IArribaManagementService _service;

        [ImportingConstructor]
        public ArribaTableRoutesApplication(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
            _service = this;

            // GET - return tables in Database
            this.Get("", this.GetTables);

            this.Get("/allBasics", this.GetAllBasics);

            this.Get("/unloadAll", this.ValidateCreateAccess, this.UnloadAll);

            // GET /table/foo - Get table information 
            this.Get("/table/:tableName", this.ValidateReadAccess, this.GetTableInformation);

            // POST /table with create table payload (Must be Writer/Owner in security directly in DiskCache folder, or identity running service)
            this.PostAsync("/table", this.ValidateCreateAccessAsync, this.ValidateBodyAsync, this.CreateNew);

            // POST /table/foo/addcolumns
            this.PostAsync("/table/:tableName/addcolumns", this.ValidateWriteAccessAsync, this.AddColumns);

            // GET /table/foo/save -- TODO: This is not ideal, think of a better pattern 
            this.Get("/table/:tableName/save", this.ValidateWriteAccess, this.Save);

            // Unload/Reload
            this.Get("/table/:tableName/unload", this.ValidateWriteAccess, this.UnloadTable);
            this.Get("/table/:tableName/reload", this.ValidateWriteAccess, this.Reload);

            // DELETE /table/foo 
            this.Delete("/table/:tableName", this.ValidateOwnerAccess, this.Drop);
            this.Get("/table/:tableName/delete", this.ValidateOwnerAccess, this.Drop);

            // POST /table/foo?action=delete
            this.Get(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);
            this.Post(new RouteSpecification("/table/:tableName", new UrlParameter("action", "delete")), this.ValidateWriteAccess, this.DeleteRows);

            // POST /table/foo/permissions/user - add permissions 
            this.PostAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Grant);

            // DELETE /table/foo/permissions/user - remove permissions from table 
            this.DeleteAsync("/table/:tableName/permissions/:scope", this.ValidateOwnerAccessAsync, this.ValidateBodyAsync, this.Revoke);

            // NOTE: _SPECIAL_ permission for localhost users, will override current auth to always be valid.
            // this enables tables recovery from local machine for matching user as the process. 
            // GET /table/foo/permissions  
            this.Get("/table/:tableName/permissions",
                    (c, r) => this.ValidateTableAccess(c, r, PermissionScope.Reader, overrideLocalHostSameUser: true),
                    this.GetTablePermissions);

            // POST /table/foo/permissions  
            this.PostAsync("/table/:tableName/permissions",
                     async (c, r) => await this.ValidateTableAccessAsync(c, r, PermissionScope.Owner, overrideLocalHostSameUser: true),
                     this.SetTablePermissions);
        }


        SecureDatabase IArribaManagementService.GetDatabaseForOwner(IPrincipal user)
        {
            if (!ValidateDatabaseAccessForUser(user, PermissionScope.Owner))
                throw new ArribaAccessForbiddenException("User has no be an owner to retrieve the database");

            return Database;
        }

        IEnumerable<string> IArribaManagementService.GetTables()
        {
            return this.Database.TableNames;
        }

        private IResponse GetTables(IRequestContext ctx, Route route)
        {
            return ArribaResponse.Ok(_service.GetTables());
        }

        private IResponse GetAllBasics(IRequestContext ctx, Route route)
        {
            IPrincipal user = ctx.Request.User;

            IDictionary<string, TableInformation> allBasics = _service.GetTablesForUser(user);

            return ArribaResponse.Ok(allBasics);
        }

        IDictionary<string, TableInformation> IArribaManagementService.GetTablesForUser(IPrincipal user)
        {
            IDictionary<string, TableInformation> allBasics = new Dictionary<string, TableInformation>();
            foreach (string tableName in this.Database.TableNames)
            {
                if (HasTableAccess(tableName, user, PermissionScope.Reader))
                {
                    allBasics[tableName] = _service.GetTableInformationForUser(tableName, user);
                }
            }

            return allBasics;
        }

        private IResponse GetTableInformation(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var tableInformation = _service.GetTableInformationForUser(tableName, ctx.Request.User);

            if (tableInformation == null)
                return ArribaResponse.NotFound();

            return ArribaResponse.Ok(tableInformation);
        }

        TableInformation IArribaManagementService.GetTableInformationForUser(string tableName, IPrincipal user)
        {
            if (!HasTableAccess(tableName, user, PermissionScope.Reader))
                return null;

            var table = this.Database[tableName];

            if (table == null)
                return null;

            TableInformation ti = new TableInformation();
            ti.Name = tableName;
            ti.PartitionCount = table.PartitionCount;
            ti.RowCount = table.Count;
            ti.LastWriteTimeUtc = table.LastWriteTimeUtc;
            ti.CanWrite = HasTableAccess(tableName, user, PermissionScope.Writer);
            ti.CanAdminister = HasTableAccess(tableName, user, PermissionScope.Owner);

            IList<string> restrictedColumns = this.Database.GetRestrictedColumns(tableName, (si) => this.IsInIdentity(user, si));
            if (restrictedColumns == null)
            {
                ti.Columns = table.ColumnDetails;
            }
            else
            {
                List<ColumnDetails> allowedColumns = new List<ColumnDetails>();
                foreach (ColumnDetails column in table.ColumnDetails)
                {
                    if (!restrictedColumns.Contains(column.Name)) allowedColumns.Add(column);
                }
                ti.Columns = allowedColumns;
            }

            return ti;
        }

        private IResponse UnloadTable(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);

            if (_service.UnloadTableForUser(tableName, ctx.Request.User))
                return ArribaResponse.Ok($"Table {tableName} unloaded");
            else
                return ArribaResponse.Forbidden($"Not able to unload table {tableName}");
        }

        bool IArribaManagementService.UnloadTableForUser(string tableName, IPrincipal user)
        {
            if (!this.HasTableAccess(tableName, user, PermissionScope.Writer))
                return false;

            this.Database.UnloadTable(tableName);
            return true;
        }

        private IResponse UnloadAll(IRequestContext ctx, Route route)
        {
            if (!_service.UnloadAllTableForUser(ctx.Request.User))
                return ArribaResponse.Forbidden("Not able to unload all tables");

            return ArribaResponse.Ok("All Tables unloaded");
        }

        bool IArribaManagementService.UnloadAllTableForUser(IPrincipal user)
        {
            if (!this.ValidateCreateAccessForUser(user))
                return false;

            this.Database.UnloadAll();
            return true;
        }

        void IArribaManagementService.DeleteTableForUser(string tableName, IPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException(tableName);

            if (!this.Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                throw new ArribaAccessForbiddenException("Operation not authorized");

            this.Database.DropTable(tableName);
        }

        private IResponse Drop(IRequestContext ctx, Route route)
        {
            var tableName = GetAndValidateTableName(route);
            var user = ctx.Request.User;

            using (ctx.Monitor(MonitorEventLevel.Information, "Drop", type: "Table", identity: tableName))
            {
                try
                {
                    _service.DeleteTableForUser(tableName, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
                return ArribaResponse.Ok("Table deleted");
            }
        }

        private IResponse GetTablePermissions(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table not found to return security for.");
            }

            var security = this.Database.Security(tableName);
            return ArribaResponse.Ok(security);
        }

        DeleteResult IArribaManagementService.DeleteTableRowsForUser(string tableName, string query, IPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Not Provided", nameof(tableName));

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Not Provided", nameof(query));

            if (!Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found");
            }

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                throw new ArribaAccessForbiddenException("User not authorized");

            var table = this.Database[tableName];
            var expression = SelectQuery.ParseWhere(query);
            var correctExpression = this.CurrentCorrectors(user).Correct(expression);
            return table.Delete(correctExpression);
        }


        private IResponse DeleteRows(IRequestContext ctx, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var query = ctx.Request.ResourceParameters["q"];
            var user = ctx.Request.User;

            try
            {
                var result = _service.DeleteTableRowsForUser(tableName, query, user);
                return ArribaResponse.Ok(result.Count);
            }
            catch(Exception ex)
            {
                return ExceptionToArribaResponse(ex);
            }
            
        }

        private async Task<IResponse> SetTablePermissions(IRequestContext request, Route route)
        {
            SecurityPermissions security = await request.Request.ReadBodyAsync<SecurityPermissions>();
            string tableName = GetAndValidateTableName(route);

            if (!this.Database.TableExists(tableName))
            {
                return ArribaResponse.NotFound("Table doesn't exist to update security for.");
            }

            // Reset table permissions and save them
            this.Database.SetSecurity(tableName, security);
            this.Database.SaveSecurity(tableName);

            return ArribaResponse.Ok("Security Updated");
        }

        private async Task<IResponse> CreateNew(IRequestContext request, Route routeData)
        {
            CreateTableRequest createTable = await request.Request.ReadBodyAsync<CreateTableRequest>();
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "Create", type: "Table", identity: createTable.TableName, detail: createTable))
            {
                try
                {
                    _service.CreateTableForUser(createTable, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
            }

            return ArribaResponse.Created(createTable.TableName);
        }

        TableInformation IArribaManagementService.CreateTableForUser(CreateTableRequest createTable, IPrincipal user)
        {
            if (createTable == null)
                throw new ArgumentNullException(nameof(createTable));

            if (string.IsNullOrWhiteSpace(createTable.TableName))
                throw new ArgumentException("Invalid table name");

            if (!ValidateCreateAccessForUser(user))
                throw new ArribaAccessForbiddenException($"Create Table access denied.");

            if (this.Database.TableExists(createTable.TableName))
            {
                throw new TableAlreadyExistsException($"Table {createTable.TableName} already exists");
            }

            var table = this.Database.AddTable(createTable.TableName, createTable.ItemCountLimit);

            // Add columns from request
            table.AddColumns(createTable.Columns);

            // Include permissions from request
            if (createTable.Permissions != null)
            {
                // Ensure the creating user is always an owner
                createTable.Permissions.Grant(IdentityScope.User, user.Identity.Name, PermissionScope.Owner);

                this.Database.SetSecurity(createTable.TableName, createTable.Permissions);
            }

            // Save, so that table existence, column definitions, and permissions are saved
            table.Save();
            this.Database.SaveSecurity(createTable.TableName);

            return _service.GetTableInformationForUser(createTable.TableName, user);
        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="columnDetails">ColumnDetails List</param>
        /// <param name="user">User requesting the operation</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TableNotFoundException"></exception>
        /// <exception cref="ArribaAccessForbiddenException"></exception>
        void IArribaManagementService.AddColumnsToTableForUser(string tableName, IList<ColumnDetails> columnDetails, IPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Not Provided", nameof(tableName));

            if (columnDetails == null || columnDetails.Count == 0)
                throw new ArgumentException("Not Provided", nameof(columnDetails));

            if (!Database.TableExists(tableName))
            {
                throw new TableNotFoundException($"Table {tableName} not found to Add Columns to.");
            }

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                throw new ArribaAccessForbiddenException("User not authorized");

            Table table = this.Database[tableName];
            table.AddColumns(columnDetails);

        }

        /// <summary>
        /// Add requested column(s) to the specified table.
        /// </summary>
        private async Task<IResponse> AddColumns(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "AddColumn", type: "Table", identity: tableName))
            {
                List<ColumnDetails> columns = await request.Request.ReadBodyAsync<List<ColumnDetails>>();
                try
                {
                    _service.AddColumnsToTableForUser(tableName, columns, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

                return ArribaResponse.Created("Added");
            }
        }

        void IArribaManagementService.ReloadTableForUser(string tableName, IPrincipal user)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Not provided", nameof(tableName));

            if (!this.Database.TableExists(tableName))
                throw new TableNotFoundException($"Table {tableName} not found");

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Reader))
                throw new ArribaAccessForbiddenException("Operation not authorized");

            this.Database.ReloadTable(tableName);

        }

        /// <summary>
        /// Reload the specified table.
        /// </summary>
        private IResponse Reload(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);
            var user = request.Request.User;

            using (request.Monitor(MonitorEventLevel.Information, "Reload", type: "Table", identity: tableName))
            {
                try
                {
                    _service.ReloadTableForUser(tableName, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

                return ArribaResponse.Ok("Reloaded");
            }
        }

        (bool, ExecutionDetails) IArribaManagementService.SaveTableForUser(string tableName, IPrincipal user, VerificationLevel verificationLevel)
        {
            bool tableSaved = false;

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Not provided", nameof(tableName));

            if (!this.Database.TableExists(tableName))
                throw new TableNotFoundException($"Table {tableName} not found");

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Writer))
                throw new ArribaAccessForbiddenException("Not authorized");

            Table table = this.Database[tableName];

            ExecutionDetails executionDetails = new ExecutionDetails();
            table.VerifyConsistency(verificationLevel, executionDetails);

            if (executionDetails.Succeeded)
            {
                table.Save();
                tableSaved = true;
            }

            return (tableSaved, executionDetails);
        }


        /// <summary>
        /// Saves the specified table.
        /// </summary>
        private IResponse Save(IRequestContext request, Route route)
        {
            string tableName = GetAndValidateTableName(route);

            using (request.Monitor(MonitorEventLevel.Information, "Save", type: "Table", identity: tableName))
            {
                try
                {
                    var saveOperation = _service.SaveTableForUser(tableName, request.Request.User, VerificationLevel.Normal);

                    if (!saveOperation.Item1)
                    {
                        return ArribaResponse.Error("Table state is inconsistent. Not saving. Restart server to reload. Errors: " + saveOperation.Item2.Errors);
                    }
                    return ArribaResponse.Ok("Saved");
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }

            }
        }

        private IResponse ExceptionToArribaResponse(Exception ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            if (ex is ArribaAccessForbiddenException)
                return ArribaResponse.Forbidden(ex.Message);

            if (ex is TableNotFoundException)
                return ArribaResponse.NotFound(ex.Message);

            return ArribaResponse.BadRequest(ex.Message);
        }

        private void CheckAuthorizationPreCondition(string tableName, SecurityIdentity securityIdentity, IPrincipal user)
        {
            ParamChecker.ThrowIfNullOrWhiteSpaced(tableName, nameof(tableName));
            ParamChecker.ThrowIfTableNotFound(this.Database, tableName);
            ParamChecker.ThrowIfNull(securityIdentity, nameof(securityIdentity));
            ParamChecker.ThrowIfNullOrWhiteSpaced(securityIdentity.Name, nameof(securityIdentity.Name));

            if (!ValidateTableAccessForUser(tableName, user, PermissionScope.Owner))
                throw new ArribaAccessForbiddenException("Operation not authorized");
        }

        void IArribaManagementService.RevokeAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user)
        {
            CheckAuthorizationPreCondition(tableName, securityIdentity, user);

            SecurityPermissions security = this.Database.Security(tableName);
            security.Revoke(securityIdentity.Scope, securityIdentity.Name, scope);

            this.Database.SaveSecurity(tableName);
        }

        private enum AuthorizationOperation
        {
            Grant = 1,
            Revoke = 2
        }

        private async Task<IResponse> ExecuteAuthorizaitonPermission(AuthorizationOperation operation, IRequestContext request, Route route)
        {
            var user = request.Request.User;
            string tableName = GetAndValidateTableName(route);
            var identity = await request.Request.ReadBodyAsync<SecurityIdentity>();

            if (!Enum.TryParse<PermissionScope>(route["scope"], true, out var scope))
            {
                return ArribaResponse.BadRequest("Unknown permission scope {0}", route["scope"]);
            }

            using (request.Monitor(MonitorEventLevel.Information, $"{operation}Permission", type: "Table", identity: tableName, detail: new { Scope = scope, Identity = identity }))
            {
                try
                {
                    if (operation == AuthorizationOperation.Grant)
                        _service.GrantAccessForUser(tableName, identity, scope, user);
                    else
                        _service.RevokeAccessForUser(tableName, identity, scope, user);
                }
                catch (Exception ex)
                {
                    return ExceptionToArribaResponse(ex);
                }
                SecurityPermissions security = this.Database.Security(tableName);
                security.Revoke(identity, scope);

                // Save permissions
                this.Database.SaveSecurity(tableName);
            }

            return ArribaResponse.Ok($"{operation} successed");
        }

        /// <summary>
        /// Revokes access to a table. 
        /// </summary>
        private async Task<IResponse> Revoke(IRequestContext request, Route route)
        {
            return await ExecuteAuthorizaitonPermission(AuthorizationOperation.Revoke, request, route);
        }

        void IArribaManagementService.GrantAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user)
        {
            CheckAuthorizationPreCondition(tableName, securityIdentity, user);

            SecurityPermissions security = this.Database.Security(tableName);
            security.Grant(securityIdentity.Scope, securityIdentity.Name, scope);

            // Save permissions
            this.Database.SaveSecurity(tableName);
        }

        /// <summary>
        /// Grants access to a table. 
        /// </summary>
        private async Task<IResponse> Grant(IRequestContext request, Route route)
        {
            return await ExecuteAuthorizaitonPermission(AuthorizationOperation.Revoke, request, route);
        }

        private static string SanitizeIdentity(string rawIdentity)
        {
            if (String.IsNullOrEmpty(rawIdentity))
            {
                throw new ArgumentException("Identity must not be empty", "rawIdentity");
            }

            return rawIdentity.Replace("/", "\\");
        }
    }
}
