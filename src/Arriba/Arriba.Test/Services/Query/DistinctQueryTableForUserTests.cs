using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Arriba.Test.Services
{
    public partial class ArribaQueryServicesTests
    {
        [DataTestMethod]
        [DataRow("foo")]
        public void DistinctQueryTableForUserTableNotFound(string tableName)
        {
            parameters.Add("col", "Value1");
            Assert.ThrowsException<TableNotFoundException>(() => _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void DistinctQueryTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DistinctQueryTableForUserParametersNotProvided(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DistinctQueryTableForUserColParametersNotProvided(string tableName)
        {
            parameters.Add("q", "name=visouza");
            Assert.ThrowsException<ArgumentException>(() => _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void DistinctQueryTableForUserUnauthorizedUser(string tableName)
        {
            parameters.Add("col", "Value1");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName, "vis", 1)]
        [DataRow(TableName, "eri", 1)]
        [DataRow(TableName, "lou", 1)]
        [DataRow(TableName, "sco", 1)]
        [DataRow(TableName, "test", 0)]
        public void DistinctQueryTableForUser(string tableName, string name, int expected)
        {
            parameters.Add("q", $"{name}");
            parameters.Add("col", "Name");

            var result = _queryService.DistinctQueryTableForUser(tableName, parameters, _telemetry, _reader);
            Assert.AreEqual(expected, result.Total);
        }
    }
}
