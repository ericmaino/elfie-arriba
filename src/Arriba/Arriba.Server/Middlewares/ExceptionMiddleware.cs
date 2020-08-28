using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Arriba.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _env;
        private const string ResponseContentType = "application/json";
        private const string LogErrorMessage = "Something went wrong: {0}";

        public ExceptionMiddleware(RequestDelegate next, IHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                if (_env.IsDevelopment())
                    throw ex;
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.BadRequest;
            context.Response.ContentType = ResponseContentType;

            if (exception is ArribaAccessForbiddenException)
                statusCode = HttpStatusCode.Unauthorized;

            if (exception is TableNotFoundException)
                statusCode = HttpStatusCode.NotFound;

            context.Response.StatusCode = (int)statusCode;

            return context.Response.WriteAsync(exception.Message);
        }
    }
}
