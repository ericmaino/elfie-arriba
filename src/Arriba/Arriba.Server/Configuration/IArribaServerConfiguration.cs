using Arriba.Configuration;
using Arriba.Telemetry;

namespace Arriba.Configuration
{
    public interface IArribaServerConfiguration : IArribaConfiguration, ISecurityConfiguration
    {
        public string FrontendBaseUrl { get; }

        IApplicationInsightsConfiguration AppInsights { get; }
    }
}
