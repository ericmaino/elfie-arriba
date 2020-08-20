using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Arriba.Test.Services
{
    public partial class ArribaQueryServicesTests
    {

        [TestMethod]
        public void IntelliSenseTableForUserParametersNotProvided()
        {
            Assert.ThrowsException<ArgumentException>(() => _queryService.IntelliSenseTableForUser(parameters, _telemetry, _reader));
        }

        [TestMethod]
        public void IntelliSenseTableForUserUnauthorizedUser()
        {
            parameters.Add("q", "test");
            Assert.ThrowsException<ArribaAccessForbiddenException>(() => _queryService.IntelliSenseTableForUser(parameters, _telemetry, _nonAuthenticatedUser));
        }

        [DataTestMethod]
        [DataRow("vis", 1)]
        [DataRow("eri", 1)]
        [DataRow("lou", 1)]
        [DataRow("sco", 1)]
        [DataRow("test", 0)]
        public void IntelliSenseTableForUser(string name, int expected)
        {
            parameters.Add("q", $"{name}");

            var result = _queryService.IntelliSenseTableForUser(parameters, _telemetry, _reader);
            Assert.AreEqual(expected, result.Suggestions.Count);
        }
    }
}
