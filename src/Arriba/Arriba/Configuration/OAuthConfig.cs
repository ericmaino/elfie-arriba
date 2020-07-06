using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Configuration
{
    public class OAuthConfig : IOAuthConfig
    {
        public OAuthConfig()
        {
        }

        public string TenantId { get; set; }

        public string AudienceId { get; set; }

        public string RedirectUrl { get; set; }

        public IList<string> Scopes { get; set; }

        public string Prompt { get; set; }

        public string AppSecret { get; set; }
    }
}
