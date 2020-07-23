using Arriba.Communication.Server.Application;
using Arriba.Model.Security;
using Arriba.Server.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        private ClaimsPrincipal GetAuthenticatedUser(string userName, PermissionScope scope)
        {
            var identity = new ClaimsIdentity(new GenericIdentity(userName, AuthenticationType));
            identity.AddClaim(new Claim(identity.RoleClaimType, scope.ToString().ToLower()));
            var user = new ClaimsPrincipal(identity);

            return user;
        }

    }
}
