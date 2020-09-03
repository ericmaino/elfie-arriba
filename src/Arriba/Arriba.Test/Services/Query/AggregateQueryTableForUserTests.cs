using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Arriba.Test.Services
{
    public partial class ArribaQueryServicesTests
    {
        [DataTestMethod]
        [DataRow("foo")]
        public void AggregateQueryTableForUserTableNotFound(string tableName)
        {
            Assert.ThrowsException<TableNotFoundException>(() => _queryService.AggregateQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("")]
        public void AggregateQueryTableForUserTableNameMissing(string tableName)
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.AggregateQueryTableForUser(tableName, parameters, _telemetry, _reader));
        }

        [DataTestMethod]
        [DataRow(TableName)]
        public void AggregateQueryTableForUserUnauthorizedUser(string tableName)
        {
            parameters.Add("col", "Name");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _queryService.AggregateQueryTableForUser(tableName, parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow(TableName, "vis", 1)]
        [DataRow(TableName, "eri", 1)]
        [DataRow(TableName, "lou", 1)]
        [DataRow(TableName, "sco", 1)]
        [DataRow(TableName, "test", 0)]
        public void AggregateQueryTableForUser(string tableName, string name, int expected)
        {
            parameters.Add("q", $"{name}");
            parameters.Add("col", "Name");

            var result = _queryService.AggregateQueryTableForUser(tableName, parameters, _telemetry, _reader);
            Assert.AreEqual(expected, result.Total);
        }
    }
}
