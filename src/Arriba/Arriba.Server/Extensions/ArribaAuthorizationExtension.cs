using Arriba.Server.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Arriba.Server.Extensions
{
    public static class ArribaAuthorizationExtension
    {
        public static IApplicationBuilder UseGetPrincipalFromToken(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ArribaIdentityMiddleware>();
        }

    }
}