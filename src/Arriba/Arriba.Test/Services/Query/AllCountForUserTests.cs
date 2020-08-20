using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Arriba.Test.Services
{
    public partial class ArribaQueryServicesTests
    {

        [TestMethod]
        public void AllCountForUserParametersNotProvided()
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.AllCountForUser(parameters, _telemetry, _reader));
        }

        [TestMethod]
        public void AllCountForUserUnauthorizedUser()
        {
            parameters.Add("q", "test");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _queryService.AllCountForUser(parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow("visouza", 1)]
        [DataRow("ericmai", 1)]
        [DataRow("louvau", 1)]
        [DataRow("scott", 1)]
        [DataRow("test", 0)]
        public void AllCountForUser(string name, int expected)
        {
            parameters.Add("q", $"{name}");

            var result = _queryService.AllCountForUser(parameters, _telemetry, _reader);
            Assert.AreEqual((ulong)expected, result.ResultsPerTable[0].Count);
        }
    }
}
