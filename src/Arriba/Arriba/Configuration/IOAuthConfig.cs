using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Configuration
{
    public interface IOAuthConfig
    {
        string TenantId { get; }
        string AudienceId { get; }
        string AppSecret { get; }
        string RedirectUrl { get; }
        IList<string> Scopes { get; }
        string Prompt { get; }
    }
}
