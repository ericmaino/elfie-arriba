using System.Security.Claims;

namespace Arriba.Server.Authorization
{
    public interface IArribaIdentityNormalizer
    {
        /// <summary>
        /// Get the IPrincipal with JWT claims and normalize its properties to the Arriba needs
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        ClaimsPrincipal NormalizeIdentity(ClaimsPrincipal identity);
    }
}
