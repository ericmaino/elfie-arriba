using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Arriba.Test.Services
{
    public partial class ArribaManagementServiceTests
    {

        [DataTestMethod]
        [DataRow(TableName)]
        public void UnloadTableByUser(string tableName)
        {
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _nonAuthenticatedUser));
            Assert.IsFalse(_service.UnloadTableForUser(tableName, _reader));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _owner));
            Assert.IsTrue(_service.UnloadTableForUser(tableName, _writer));
        }
    }
}
