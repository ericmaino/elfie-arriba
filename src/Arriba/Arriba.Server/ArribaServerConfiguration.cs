using Arriba.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arriba.Server
{
    public class ArribaServerConfiguration : IArribaConfiguration
    {        

        public string ArribaTable => "";

        public OAuthConfig OAuthConfig { get; set; }

        public ArribaServerConfiguration()
        {
            OAuthConfig = new OAuthConfig();
        }
    }
}
