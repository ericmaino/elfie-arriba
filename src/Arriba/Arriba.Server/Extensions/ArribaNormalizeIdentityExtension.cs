using Arriba.Server.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Arriba.Extensions
{
    public static class ArribaNormalizeIdentityExtension
    {
        public static IApplicationBuilder UseArribaNormalizeIdentity(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ArribaNormalizeIdentityMiddleware>();
        }

    }
}