using Arriba.Communication.Server.Authorization;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Types;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Arriba.Communication.Server.Application
{
    internal class ArribaManagementService : IArribaManagementService
    {
        private readonly SecureDatabase _database;
        private readonly IArribaAuthorization _arribaAuthorization;

        public ArribaManagementService(SecureDatabase secureDatabase)
        {
            _database = secureDatabase;
            _arribaAuthorization = new ArribaAuthorization(_database);
        }

        public void AddColumnsToTableForUser(string tableName, IList<ColumnDetails> columnDetails, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public TableInformation CreateTableForUser(CreateTableRequest table, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public void DeleteTableForUser(string tableName, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public DeleteResult DeleteTableRowsForUser(string tableName, string query, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public SecureDatabase GetDatabaseForOwner(IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public TableInformation GetTableInformationForUser(string tableName, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetTables()
        {
            return this._database.TableNames;
        }

        public IDictionary<string, TableInformation> GetTablesForUser(IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public void GrantAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public void ReloadTableForUser(string tableName, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public void RevokeAccessForUser(string tableName, SecurityIdentity securityIdentity, PermissionScope scope, IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public (bool, ExecutionDetails) SaveTableForUser(string tableName, IPrincipal user, VerificationLevel verificationLevel)
        {
            throw new NotImplementedException();
        }

        public bool UnloadAllTableForUser(IPrincipal user)
        {
            throw new NotImplementedException();
        }

        public bool UnloadTableForUser(string tableName, IPrincipal user)
        {
            throw new NotImplementedException();
        }
    }
}
