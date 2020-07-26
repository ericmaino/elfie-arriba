using Arriba.Model;
using Arriba.ParametersCheckers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Arriba.Test.ParameterChecker
{
    [TestClass]
    public class ParamCheckerTests
    {

        [TestMethod]
        public void ThrowArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => ParamChecker.ThrowIfNull<string>(null, "paramName"));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void ThrowArgumentException(string value)
        {
            Assert.ThrowsException<ArgumentException>(() => ParamChecker.ThrowIfNullOrWhiteSpaced(value, nameof(value)));
        }

        [DataTestMethod]
        [DataRow("foo")]
        [DataRow("bar")]
        public void ThrowTableNotFoundException(string tableName)
        {
            Assert.ThrowsException<ArgumentNullException>(() => ParamChecker.ThrowIfTableNotFound(null, tableName));

            var db = new SecureDatabase();
            Assert.ThrowsException<TableNotFoundException>(() => ParamChecker.ThrowIfTableNotFound(db, tableName));
        }

        [DataTestMethod]
        [DataRow("People")]        
        public void ThrowTableAlreadyExistsException(string tableName)
        {
            Assert.ThrowsException<ArgumentNullException>(() => ParamChecker.ThrowIfTableAlreadyExists(null, tableName));

            var db = new SecureDatabase();
            var table = new Table(tableName, 10);
            table.Save();
            Assert.ThrowsException<TableAlreadyExistsException>(() => ParamChecker.ThrowIfTableAlreadyExists(db, tableName));
            table.Drop();
        }
    }
}
