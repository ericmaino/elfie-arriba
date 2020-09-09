using Arriba.Security.OAuth;
using Arriba.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace Arriba.Extensions
{
    public static class OAuthExtension
    {
        public static IServiceCollection AddOAuth(this IServiceCollection services, IArribaServerConfiguration serverConfig)
        {
            services.AddSingleton<IAuthorizationHandler, AllowAnonymous>();

            if (!serverConfig.EnabledAuthentication)
                return services;

            var azureTokens = AzureJwtTokenFactory.CreateAsync(serverConfig.OAuthConfig).Result;
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(azureTokens.Configure);

            var jwtBearerPolicy = new AuthorizationPolicyBuilder()
               .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
               .RequireAuthenticatedUser()
               .Build();

            services.AddAuthorization(auth =>
            {
                auth.DefaultPolicy = jwtBearerPolicy;
            });

            return services;
        }
    }

    public class AllowAnonymous : IAuthorizationHandler
    {
        private readonly IArribaServerConfiguration _serverConfiguration;

        public AllowAnonymous(IArribaServerConfiguration serverConfiguration)
        {
            _serverConfiguration = serverConfiguration;
        }

        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (!_serverConfiguration.EnabledAuthentication)
            {
                foreach (IAuthorizationRequirement requirement in context.PendingRequirements.ToList())
                    if (requirement is DenyAnonymousAuthorizationRequirement)
                        context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}
