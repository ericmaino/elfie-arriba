using Arriba.Configuration;
using Arriba.Security.OAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Arriba.Server.Extensions
{
    public static class OAuthExtension
    { 
        public static IServiceCollection AddOAuth(this IServiceCollection services, IArribaServerConfiguration serverConfig)
        {
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

            services.AddSingleton((_) => serverConfig.OAuthConfig);

            return services;
        }
    }
}
