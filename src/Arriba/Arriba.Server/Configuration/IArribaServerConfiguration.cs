using Arriba.Configuration;
using Arriba.Telemetry;

namespace Arriba.Configuration
{
    public interface IArribaServerConfiguration : IArribaConfiguration
    {
        public bool EnabledAuthentication { get; }
        public IOAuthConfig OAuthConfig { get; }
        public string FrontendBaseUrl { get; }

        IApplicationInsightsConfiguration AppInsights { get; }
    }
}
