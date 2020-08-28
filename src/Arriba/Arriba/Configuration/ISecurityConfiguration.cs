using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Configuration
{
    public interface ISecurityConfiguration
    {
        bool EnabledAuthentication { get; }
        IOAuthConfig OAuthConfig { get; }
    }
}
