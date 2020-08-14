using Microsoft.AspNetCore.Builder;
using Arriba.Middlewares;

namespace Arriba.Extensions
{
    public static class ExceptionExtensions
    {
        public static void UseArribaExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
