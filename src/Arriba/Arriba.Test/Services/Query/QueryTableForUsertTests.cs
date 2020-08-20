using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Arriba.Test.Services
{
    public partial class ArribaQueryServicesTests
    {
        [DataTestMethod]
        [DataRow("foo")]
        public void QueryTableForUserTableNotFound(string tableName)
        {
            parameters.Add("Test", "Value1");
            Assert.ThrowsException<TableNotFoundException>(() => _queryService.QueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void QueryTableForUserTableNameMissing(string tableName)
        {
            parameters.Add("Test", "Value1");
            Assert.ThrowsException<ArgumentException>(() => _queryService.QueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void QueryTableForUserParametersNotProvided(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.QueryTableForUser(tableName, parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void QueryTableForUserUnauthorizedUser(string tableName)
        {
            parameters.Add("Test", "Value1");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _queryService.QueryTableForUser(tableName, parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName, "visouza", 1)]
        [DataRow(TableName, "ericmai", 1)]
        [DataRow(TableName, "louvau", 1)]
        [DataRow(TableName, "scott", 1)]
        [DataRow(TableName, "test", 0)]
        public void QueryTableForUser(string tableName, string name, int expected)
        {
            parameters.Add("q", $"Name={name}");

            var result = _queryService.QueryTableForUser(tableName, parameters, _telemetry, _reader);
            Assert.AreEqual(expected, result.CountReturned);
        }
    }
}
