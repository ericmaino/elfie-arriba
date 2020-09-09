// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Arriba.Server.Hosting
{
    using Arriba.Model;
    using Arriba.Model.Security;
    using System.Linq;

    /// <summary>
    /// Represents a singleton database export
    /// </summary>
    public class DatabaseFactory
    {
        private static SecureDatabase s_database;

        public DatabaseFactory()
        {
            if (s_database == null)
            {
                s_database = new SecureDatabase();

                //HACK: As the crawler don't have the AppRoles available at the JWT Token as claims
                //needed to grant for each role (PermissionScope) the roles name must use the same name as the Scope.
                s_database.TableNames.ToList().ForEach(table =>
                {
                    var security = s_database.Security(table);
                    security.Grant(IdentityScope.Group, PermissionScope.Owner.ToString(), PermissionScope.Owner);
                    security.Grant(IdentityScope.Group, PermissionScope.Writer.ToString(), PermissionScope.Writer);
                    security.Grant(IdentityScope.Group, PermissionScope.Reader.ToString(), PermissionScope.Reader);
                });
            }
        }

        public SecureDatabase GetDatabase()
        {
            return s_database;
        }
    }
}
