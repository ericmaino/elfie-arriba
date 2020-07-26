using Arriba.Model;
using System;
using System.Collections;
using System.Security.Principal;

namespace Arriba.ParametersCheckers
{

    public static class ParamChecker
    {
        public static void ThrowIfNull<T>(T value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        public static void ThrowIfNull(this IPrincipal user, string paramName)
        {
            if (user is null)
                throw new ArgumentNullException(paramName);
        }

        public static void ThrowIfNullOrWhiteSpaced(this string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Not Provided", paramName);
        }

        public static void ThrowIfTableNotFound(this Database db, string tableName)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (!db.TableExists(tableName))
                throw new TableNotFoundException($"Table {tableName} not found");
        }

        public static void ThrowIfTableAlreadyExists(this Database db, string tableName)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (db.TableExists(tableName))
                throw new TableAlreadyExistsException($"Table {tableName} not found");
        }
    }
}
