using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.Claims;
using Arriba.Server.Models;

namespace Arriba.Server.Middlewares
{
    public class ArribaIdentityMiddleware
    {
        private const string AuthorizationKey = "authorization";
        private const string BearerTokenPrefix = "Bearer";
        private const string AuthenticationType = "JWTTokenGenerated";
        private readonly RequestDelegate _next;

        public ArribaIdentityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IArribaServerConfiguration serverConfiguration)
        {
            if (!serverConfiguration.EnabledAuthentication)
                await _next(httpContext);

            var authHeader = httpContext.Request.Headers[AuthorizationKey];
            var bearerToken = authHeader.Where(s => s.StartsWith(BearerTokenPrefix)).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(bearerToken))
                await _next(httpContext);

            bearerToken = bearerToken.Substring($"{BearerTokenPrefix} ".Length);

            var user = CreatePrincipal(bearerToken);
            httpContext.User = user;

            await _next(httpContext);
        }

        private ClaimsPrincipal CreatePrincipal(string accessToken)
        {
            var parts = accessToken.Split('.');

            if (parts.Length != 3)
                return null;

            var payload = parts[1];            

            var token = TryToGetTokenObject(payload);
            ClaimsPrincipal user = GetPrincipalFromToken(token);

            return user;
        }

        private ClaimsPrincipal GetPrincipalFromToken(Token token)
        {
            if (token == null)
                return null;

            var identity = new ClaimsIdentity(new GenericIdentity(token.Name, AuthenticationType));
            token.Roles.ToList().ForEach(role =>
            {
                var claim = new Claim(identity.RoleClaimType, role.ToLower());
                identity.AddClaim(claim);
            });

            var user = new ClaimsPrincipal(identity);
            return user;
        }

        private Token TryToGetTokenObject(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            var decodedPayload = Base64UrlEncoder.Decode(payload);
            Trace.WriteLine($"decodedPayload: {decodedPayload}");

            return Token.FromJson(decodedPayload);
        }
    }
}
