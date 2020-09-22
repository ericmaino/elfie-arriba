using Arriba.Server.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Arriba.Server.Middlewares
{
    public class ArribaNormalizeIdentityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IArribaIdentityNormalizer _arribaIdentityNormalizer;

        public ArribaNormalizeIdentityMiddleware(RequestDelegate next, IArribaIdentityNormalizer arribaIdentityNormalizer)
        {
            _next = next;
            _arribaIdentityNormalizer = arribaIdentityNormalizer;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.User.Identity.IsAuthenticated)
            {
                var user = _arribaIdentityNormalizer.NormalizeIdentity(httpContext.User);
                httpContext.User = user;
            }

            await _next(httpContext);
        }
    }
}
