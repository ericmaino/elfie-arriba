using Microsoft.AspNetCore.Builder;
using Arriba.Server.Middlewares;

namespace Arriba.Server.Extensions
{
    public static class ExceptionExtensions
    {
        public static void UseArribaExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
