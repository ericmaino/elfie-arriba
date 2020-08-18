using Arriba.Model;
using System;
using System.Collections.Specialized;
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

        public static void ThrowIfNullOrWhiteSpaced(this string value, string paramName, string message = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (string.IsNullOrWhiteSpace(message))
                    throw new ArgumentException("Not Provided", paramName);
                throw new ArgumentException(message);
            }
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

        public static void ThrowIfNullOrEmpty(this NameValueCollection value, string paramName)
        {
            if (value == null || value.Count == 0)
                throw new ArgumentException("Not Provided", paramName);
        }
    }
}
