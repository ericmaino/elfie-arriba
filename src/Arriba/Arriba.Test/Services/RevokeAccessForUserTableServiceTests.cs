using Arriba.Model.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {
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
