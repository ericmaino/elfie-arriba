using Arriba.Communication.Server.Application;
using Arriba.Model.Security;
using Arriba.Server.Hosting;
using Arriba.Types;
using Microsoft.AspNetCore.Http.Connections;
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

        private readonly ClaimsPrincipal _nonAuthenticatedUser;
        private readonly ClaimsPrincipal _owner;
        private readonly ClaimsPrincipal _reader;
        private readonly ClaimsPrincipal _writer;

        private readonly IArribaManagementService _service;
        private readonly DatabaseFactory _databaseFactory;
        public ArribaManagementServiceTests()
        {
            _nonAuthenticatedUser = new ClaimsPrincipal();
            _reader = GetAuthenticatedUser("user1", PermissionScope.Reader);
            _writer = GetAuthenticatedUser("user2", PermissionScope.Writer);
            _owner = GetAuthenticatedUser("user3", PermissionScope.Owner);

            _databaseFactory = new DatabaseFactory();
            var factory = new ArribaManagementServiceFactory(_databaseFactory);

            _service = factory.CreateArribaManagementService();
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
            var db = new SecureDatabase();
            var tableName = $"{table}_{user.Identity.Name}";

            DeleteTable(db, tableName);

            var tableOwner = _service.CreateTableForUser(new CreateTableRequest(tableName, 1000), user);
            Assert.IsNotNull(tableOwner);
            Assert.IsTrue(tableOwner.CanAdminister);
            Assert.IsTrue(tableOwner.CanWrite);

            DeleteTable(db, tableName);

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

    }
}
