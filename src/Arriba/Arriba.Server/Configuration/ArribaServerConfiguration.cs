using Arriba.Configuration;

namespace Arriba.Configuration
{
    public class ArribaServerConfiguration : IArribaServerConfiguration
    {        
        public IOAuthConfig OAuthConfig { get; set; }
        
        public string FrontendBaseUrl { get; set; }

        public bool EnabledAuthentication { get; set; }

        public ArribaServerConfiguration()
        {
            OAuthConfig = new OAuthConfig();
        }
    }
}
