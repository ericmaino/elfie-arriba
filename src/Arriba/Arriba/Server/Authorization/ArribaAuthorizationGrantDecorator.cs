using System.Security.Principal;
using Arriba.Configuration;
using Arriba.Diagnostics.Tracing;
using Arriba.Model;
using Arriba.Model.Security;
using Arriba.Server.Authentication;

namespace Arriba.Server.Authorization
{
    public class ArribaAuthorizationGrantDecorator : IArribaAuthorization
    {

        public readonly ISecurityConfiguration _securityConfiguration;
        public readonly IArribaAuthorization _authorization;
        public readonly ILoggingContext _log;

        public ArribaAuthorizationGrantDecorator(SecureDatabase database, ClaimsAuthenticationService claims, ISecurityConfiguration securityConfiguration, ILoggingContextFactory log)
        {
            _authorization = new ArribaAuthorization(database, claims);
            _securityConfiguration = securityConfiguration;
            _log = log.Initialize<ArribaAuthorizationGrantDecorator>();
        }

        private bool OverrideGrantedPermission(bool hasPermission)
        {
            if (!_securityConfiguration.EnabledAuthentication)
            {
                if (!hasPermission)
                {
                    _log.TrackPermissionOverride();
                }

                return true;
            }
            return hasPermission;
        }

        public bool IsInIdentity(IPrincipal currentUser, SecurityIdentity targetUserOrGroup)
        {
            return OverrideGrantedPermission(_authorization.IsInIdentity(currentUser, targetUserOrGroup));
        }

        public bool ValidateCreateAccessForUser(IPrincipal user)
        {
            return OverrideGrantedPermission(_authorization.ValidateCreateAccessForUser(user));
        }

        public bool ValidateDatabaseAccessForUser(IPrincipal user, PermissionScope scope)
        {
            return OverrideGrantedPermission(_authorization.ValidateDatabaseAccessForUser(user, scope));
        }

        public bool ValidateTableAccessForUser(string tableName, IPrincipal user, PermissionScope scope)
        {
            return OverrideGrantedPermission(_authorization.ValidateTableAccessForUser(tableName, user, scope));
        }
    }
}
