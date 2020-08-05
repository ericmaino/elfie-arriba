using Arriba.Communication.Model;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Model.Security;
using Arriba.ParametersCheckers;
using System.Collections.Generic;
using System.Linq;

namespace Arriba.Communication.Server.Application
{
    public class ArribaManagementServiceFactory
    {
        private const string Table_People = "People";

        private readonly SecureDatabase secureDatabase;

        public ArribaManagementServiceFactory(SecureDatabase secureDatabase)
        {
            ParamChecker.ThrowIfNull(secureDatabase, nameof(secureDatabase));

            this.secureDatabase = secureDatabase;
        }

        public IArribaManagementService CreateArribaManagementService(string userAliasCorrectorTable = "")
        {
            if (string.IsNullOrWhiteSpace(userAliasCorrectorTable))
                userAliasCorrectorTable = Table_People;

            var correctors = new CompositionComposedCorrectors(new TodayCorrector(), new UserAliasCorrector(secureDatabase[userAliasCorrectorTable]));

            //HACK: As the crawler don't have the AppRoles available at the JWT Token as claims
            //needed to grant for each role (PermissionScope) the roles name must use the same name as the Scope.
            secureDatabase.TableNames.ToList().ForEach(table =>
            {
                var security = secureDatabase.Security(table);
                security.Grant(IdentityScope.Group, PermissionScope.Reader.ToString(), PermissionScope.Reader);
                security.Grant(IdentityScope.Group, PermissionScope.Writer.ToString(), PermissionScope.Writer);
                security.Grant(IdentityScope.Group, PermissionScope.Owner.ToString(), PermissionScope.Owner);                
            });
            

            return new ArribaManagementService(secureDatabase, correctors);
        }
    }
}