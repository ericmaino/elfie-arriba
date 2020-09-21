using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Arriba.Model.Security;
using Arriba.ParametersCheckers;

namespace Arriba.Server.Authorization
{
    public class ArribaIdentityNormalizer : IArribaIdentityNormalizer
    {
        private const string ClaimTypeName = "name";

        public ClaimsPrincipal NormalizeIdentity(ClaimsPrincipal user)
        {
            user.ThrowIfNull(nameof(user));
            user.Identity.ThrowIfNull(nameof(user.Identity));
            user.Identity.AuthenticationType.ThrowIfNullOrWhiteSpaced(nameof(user.Identity.AuthenticationType));

            var nameClaim = user.Claims.Where(c => c.Type == ClaimTypeName).FirstOrDefault();
            string userName = nameClaim != null ? nameClaim.Value : string.Empty;

            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = user.Identity.Name;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArribaIdentityNormalizerException("Not able to get username through Claims or Identity!");
            }

            //Check for roles claims
            var roleClaim = user.Claims.Where(c => c.Type == ClaimTypes.Role && 
                                             (c.Value == PermissionScope.Reader.ToString() || 
                                              c.Value == PermissionScope.Writer.ToString() ||
                                              c.Value == PermissionScope.Owner.ToString())).FirstOrDefault();
            if (roleClaim == null)
            {
                throw new ArribaIdentityNormalizerException("Any Arriba Permission Scopes is defined on Role Claims!");
            }

            var newIdentity = new ClaimsIdentity(new GenericIdentity(userName, user.Identity.AuthenticationType), user.Claims);

            return new ClaimsPrincipal(newIdentity);
        }
    }
}
