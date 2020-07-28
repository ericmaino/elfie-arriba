using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Model.Column;
using Arriba.Model.Query;
using Arriba.Model.Security;
using Arriba.Server.Hosting;
using Arriba.Structures;
using Arriba.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Arriba.Test.Services
{
    [TestClass]
    public class ArribaManagementServiceTests
    {
        //Without specifying a type identity.IsAuthenticated always returns false
        private const string AuthenticationType = "TestAuthenticationType";
        private const string TableName = "Users";

        private readonly SecureDatabase _db;

        private readonly ClaimsPrincipal _nonAuthenticatedUser;
        private readonly ClaimsPrincipal _owner;
        private readonly ClaimsPrincipal _reader;
        private readonly ClaimsPrincipal _writer;

        private readonly IArribaManagementService _service;
        private readonly DatabaseFactory _databaseFactory;
        public ArribaManagementServiceTests()
        {
            CreateTestDatabase(TableName);

            _nonAuthenticatedUser = new ClaimsPrincipal();
            _reader = GetAuthenticatedUser("user1", PermissionScope.Reader);
            _writer = GetAuthenticatedUser("user2", PermissionScope.Writer);
            _owner = GetAuthenticatedUser("user3", PermissionScope.Owner);

            _databaseFactory = new DatabaseFactory();
            var factory = new ArribaManagementServiceFactory(_databaseFactory);

            _service = factory.CreateArribaManagementService();
            _db = _service.GetDatabaseForOwner(_owner);
        }

        private void CreateTestDatabase(string tableName)
        {
            SecureDatabase db = new SecureDatabase();

            if (db.TableExists(tableName)) db.DropTable(tableName);

            Table t = db.AddTable(tableName, 100);
            t.AddColumn(new ColumnDetails("ID", "int", null, null, true));
            t.AddColumn(new ColumnDetails("Name", "string", ""));

            DataBlock b = new DataBlock(new string[] { "ID", "Name" }, 4,
                new Array[]
                {
                    new int[] { 1, 2, 3, 4},
                    new string[] { "visouza", "ericmai", "louvau", "scott"}
                });
            t.AddOrUpdate(b);
            t.Save();

            //For database
            SetSecurityGroup(db, string.Empty);
            //For table
            SetSecurityGroup(db, tableName);
        }

        private void SetSecurityGroup(SecureDatabase db, string tableName)
        {
            SecurityPermissions security = db.Security(tableName);

            security.Readers.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Reader.ToString()));
            security.Writers.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Writer.ToString()));
            security.Owners.Add(new SecurityIdentity(IdentityScope.Group, PermissionScope.Owner.ToString()));
            db.SaveSecurity(tableName);
        }

        private void DeleteTable(SecureDatabase db, string tableName)
        {
            if (db.TableExists(tableName))
                db.DropTable(tableName);
        }

        [TestCleanup]
        public void DeleteDatabaseTestTables()
        {
            foreach (var table in _db.TableNames)
            {
                DeleteTable(_db, table);
            }
        }

        [TestMethod]
        public void GetTables()
        {
            var tables = _service.GetTables();
            Assert.IsNotNull(tables);
        }

        [TestMethod]
        public void GetTablesByUser()
        {
            var tables = _service.GetTablesForUser(_nonAuthenticatedUser);
            Assert.IsNotNull(tables);
            Assert.AreEqual(0, tables.Count);

            var tablesReader = _service.GetTablesForUser(_reader);
            Assert.IsNotNull(tablesReader);
            Assert.IsTrue(tablesReader.Count <= _service.GetTables().Count());

            var tablesWriter = _service.GetTablesForUser(_writer);
            Assert.IsNotNull(tablesWriter);

            var tablesOwner = _service.GetTablesForUser(_owner);
            Assert.IsNotNull(tablesOwner);
            Assert.AreEqual(tablesOwner.Count, _service.GetTables().Count());

        }

        [TestMethod]
        public void GetTablesByNonAuthenticatedUser()
        {
            var tables = _service.GetTablesForUser(_nonAuthenticatedUser);
            Assert.IsNotNull(tables);
            Assert.AreEqual(0, tables.Count);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void UnloadTableByUser(string tableName)
        {
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _nonAuthenticatedUser));
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _reader));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _owner));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _writer));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void GetTableInformationForUser(string tableName)
        {
            var table = _service.GetTableInformationForUser(tableName, _nonAuthenticatedUser);
            Assert.IsNull(table);

            var tableOwner = _service.GetTableInformationForUser(tableName, _owner);
            Assert.IsNotNull(tableOwner);
            Assert.IsTrue(tableOwner.CanAdminister);
            Assert.IsTrue(tableOwner.CanWrite);

            var tableWriter = _service.GetTableInformationForUser(tableName, _writer);
            Assert.IsNotNull(tableWriter);
            Assert.IsFalse(tableWriter.CanAdminister);
            Assert.IsTrue(tableWriter.CanWrite);

            var tableReader = _service.GetTableInformationForUser(tableName, _reader);
            Assert.IsNotNull(tableReader);
            Assert.IsFalse(tableReader.CanAdminister);
            Assert.IsFalse(tableReader.CanWrite);
        }

        [TestMethod]
        public void CreateTableForUserNullObject()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _nonAuthenticatedUser));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _reader));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _writer));
            Assert.ThrowsException<ArgumentNullException>(() => _service.CreateTableForUser(null, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        public void CreateTableForUserWithInvalidName(string tableName)
        {
            var tableRequest = new CreateTableRequest(tableName, 1000);

            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _nonAuthenticatedUser));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _reader));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _writer));
            Assert.ThrowsException<ArgumentException>(() => _service.CreateTableForUser(tableRequest, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserNotAuthorized(string tableName)
        {
            var tableRequest = new CreateTableRequest($"{tableName}_notauthorized", 1000);

            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.CreateTableForUser(tableRequest, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.CreateTableForUser(tableRequest, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserOwner(string tableName)
        {
            CreateTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserWriter(string tableName)
        {
            CreateTableForUser(tableName, _writer);
        }

        private void CreateTableForUser(string table, IPrincipal user)
        {
            var tableName = $"{table}_{user.Identity.Name}";

            DeleteTable(_db, tableName);

            var tableOwner = _service.CreateTableForUser(new CreateTableRequest(tableName, 1000), user);
            Assert.IsNotNull(tableOwner);
            Assert.IsTrue(tableOwner.CanAdminister);
            Assert.IsTrue(tableOwner.CanWrite);

            DeleteTable(_db, tableName);

        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void CreateTableForUserTableAlreadyExists(string tableName)
        {
            Assert.ThrowsException<TableAlreadyExistsException>(() => _service.CreateTableForUser(new CreateTableRequest(tableName, 1000), _owner));
        }

        private ClaimsPrincipal GetAuthenticatedUser(string userName, PermissionScope scope)
        {
            var identity = new ClaimsIdentity(new GenericIdentity(userName, AuthenticationType));
            identity.AddClaim(new Claim(identity.RoleClaimType, scope.ToString().ToLower()));
            var user = new ClaimsPrincipal(identity);

            return user;
        }

        [DataTestMethod]
        [DataRow("foo")]
        public void AddColumnsToTableForUserTableDoesntExist(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<TableNotFoundException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _owner));
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow(null)]
        public void AddColumnsToTableForUserTableNameMissing(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<ArgumentException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserNotAuthorized(string tableName)
        {
            var columnList = GetColumnDetailsList();
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.AddColumnsToTableForUser(tableName, columnList, _reader));
        }

        private void CheckTableColumnsQuantity(string tableName, int expected)
        {
            var table = _db[tableName];

            Assert.AreEqual(expected, table.ColumnDetails.Count);
        }

        private void AddColumnsToTableForUser(string tableName, IPrincipal user)
        {
            CheckTableColumnsQuantity(tableName, 2);
            var columnList = GetColumnDetailsList();
            _service.AddColumnsToTableForUser(tableName, columnList, user);
            CheckTableColumnsQuantity(tableName, 3);
        }

        private static List<ColumnDetails> GetColumnDetailsList()
        {
            var columnList = new List<ColumnDetails>();
            columnList.Add(new ColumnDetails("Column", "string", ""));
            return columnList;
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserOwner(string tableName)
        {
            AddColumnsToTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AddColumnsToTableForUserWriter(string tableName)
        {
            AddColumnsToTableForUser(tableName, _writer);
        }

        [DataTestMethod]
        [DataRow("foo")]
        public void SaveTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.SaveTableForUser(tableName, _owner, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void SaveTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.SaveTableForUser(tableName, _owner, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.SaveTableForUser(tableName, _nonAuthenticatedUser, VerificationLevel.Normal));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.SaveTableForUser(tableName, _reader, VerificationLevel.Normal));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUserInconsistent(string tableName)
        {
            CheckTableColumnsQuantity(tableName, 2);
            _service.AddColumnsToTableForUser(tableName, GetColumnDetailsList(), _owner);

            var result = _service.SaveTableForUser(tableName, _owner, VerificationLevel.Normal);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Item1);
            Assert.IsFalse(result.Item2.Succeeded);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void SaveTableForUser(string tableName)
        {
            var result = _service.SaveTableForUser(tableName, _owner, VerificationLevel.Normal);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Item1);
            Assert.IsTrue(result.Item2.Succeeded);
        }

        [DataTestMethod]
        [DataRow("foo")]
        public void ReloadTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.ReloadTableForUser(tableName, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void ReloadTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.ReloadTableForUser(tableName, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void ReloadTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.ReloadTableForUser(tableName, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void ReloadTableForUser(string tableName)
        {
            _service.ReloadTableForUser(tableName, _reader);
            _service.ReloadTableForUser(tableName, _writer);
            _service.ReloadTableForUser(tableName, _owner);
        }


        [DataTestMethod]
        [DataRow("foo")]
        public void DeleteTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.DeleteTableForUser(tableName, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void DeleteTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.DeleteTableForUser(tableName, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.DeleteTableForUser(tableName, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserOwner(string tableName)
        {
            DeleteTableForUser(tableName, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableForUserWriter(string tableName)
        {
            DeleteTableForUser(tableName, _owner);
        }

        private void DeleteTableForUser(string tableName, IPrincipal user)
        {
            _service.DeleteTableForUser(tableName, user);
        }


        [DataTestMethod]
        [DataRow("foo")]
        public void DeleteTableRowsForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void DeleteTableRowsForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DeleteTableRowsForUserUnauthorizedUser(string tableName)
        {
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.DeleteTableRowsForUser(tableName, "ID = 1", _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName, "ID = 1 OR Name = Vinicius")]
        [DataRow(TableName, "ID = 2")]
        [DataRow(TableName, "ID = 99")]
        public void DeleteTableRowsForUserOwner(string tableName, string query)
        {
            DeleteTableRowsForUser(tableName, query, _owner);
        }

        [DataTestMethod]
        [DataRow(TableName, "ID = 1 OR Name = Vinicius")]
        [DataRow(TableName, "ID = 2")]
        [DataRow(TableName, "ID = 99")]
        public void DeleteTableRowsForUserWriter(string tableName, string query)
        {
            DeleteTableRowsForUser(tableName, query, _owner);
        }

        private void DeleteTableRowsForUser(string tableName, string query, IPrincipal user)
        {
            var table = _db[tableName];
            var countBefore = table.Count;
            var result = _service.DeleteTableRowsForUser(tableName, query, user);
            if (result.Count > 0)
                Assert.AreEqual(countBefore - result.Count, table.Count);
            else
                Assert.AreEqual(countBefore, table.Count);
        }

        [DataTestMethod]
        [DataRow("foo")]
        public void GrantAccessForUserTableNotFound(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<TableNotFoundException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void GrantAccessForUserTableNameMissing(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, " ")]
        public void GrantAccessForUserSecurityIdendityMissing(string tableName, IdentityScope scope, string identityName)
        {
            var identity = new SecurityIdentity(scope, identityName);
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void GrantAccessForUserUnauthorizedUser(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _reader));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _writer));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, "group1", PermissionScope.Reader)]
        [DataRow(TableName, IdentityScope.User, "user5", PermissionScope.Writer)]
        [DataRow(TableName, IdentityScope.Group, "group4", PermissionScope.Owner)]
        public void GrantAccessForUserOwner(string tableName, IdentityScope scope, string identityName, PermissionScope permissionScope)
        {
            var countBefore = GetPermissionScopeQuantity(tableName, permissionScope);
            var identity = new SecurityIdentity(scope, identityName);
            _service.GrantAccessForUser(tableName, identity, permissionScope, _owner);
            var countAfter = GetPermissionScopeQuantity(tableName, permissionScope);
            Assert.IsTrue(countBefore < countAfter);
        }

        private int GetPermissionScopeQuantity(string tableName, PermissionScope permissionScope)
        {
            var security = _db.Security(tableName);
            switch (permissionScope)
            {
                case PermissionScope.Reader: return security.Readers.Count;
                case PermissionScope.Writer: return security.Writers.Count;
                case PermissionScope.Owner: return security.Owners.Count;
            }
            throw new ArribaException("Permission Scope not handled!");
        }


        [DataTestMethod]
        [DataRow("foo")]
        public void RevokeAccessForUserTableNotFound(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<TableNotFoundException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void RevokeAccessForUserTableNameMissing(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, " ")]
        public void RevokeAccessForUserSecurityIdendityMissing(string tableName, IdentityScope scope, string identityName)
        {
            var identity = new SecurityIdentity(scope, identityName);
            Assert.ThrowsException<ArgumentException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _owner));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void RevokeAccessForUserUnauthorizedUser(string tableName)
        {
            var identity = new SecurityIdentity(IdentityScope.Group, "table readers");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _nonAuthenticatedUser));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _reader));
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _service.GrantAccessForUser(tableName, identity, PermissionScope.Reader, _writer));
        }

        [DataTestMethod]
        [DataRow(TableName, IdentityScope.Group, "reader", PermissionScope.Reader)]
        [DataRow(TableName, IdentityScope.User, "user2", PermissionScope.Writer)]
        [DataRow(TableName, IdentityScope.Group, "writer", PermissionScope.Writer)]
        [DataRow(TableName, IdentityScope.Group, "user1", PermissionScope.Reader)]
        public void RevokeAccessForUserOwner(string tableName, IdentityScope scope, string identityName, PermissionScope permissionScope)
        {
            var countBefore = GetPermissionScopeQuantity(tableName, permissionScope);
            var identity = new SecurityIdentity(scope, identityName);
            _service.RevokeAccessForUser(tableName, identity, permissionScope, _owner);
            var countAfter = GetPermissionScopeQuantity(tableName, permissionScope);
            Assert.IsTrue(countBefore >= countAfter);            
        }

    }
}
