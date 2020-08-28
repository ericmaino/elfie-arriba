using System;
using System.Collections.Generic;
using System.Composition;
using Arriba.Caching;
using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Communication.Server.Application;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Server;
using Arriba.Server.Application;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;

namespace Arriba.Composition
{
    [Export(typeof(IRoutedApplication))]
    class ArribaImportApplicationExport : ArribaImportApplication
    {
        [ImportingConstructor]
        public ArribaImportApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth, ISecurityConfiguration securityConfiguration)
            : base(f, auth, securityConfiguration)
        {
        }
    }

    [Export(typeof(ClaimsAuthenticationService)), Shared]
    class ClaimsAuthenticationServiceExport : ClaimsAuthenticationService
    {
        [ImportingConstructor]
        public ClaimsAuthenticationServiceExport(IObjectCacheFactory factory)
            : base(factory)
        {
        }
    }

    [Export(typeof(IArribaManagementService)), Shared]
    class ArribaManagementServiceExport : ArribaManagementService
    {
        [ImportingConstructor]
        public ArribaManagementServiceExport(SecureDatabase secureDatabase, ICorrector corrector, ClaimsAuthenticationService claims, ISecurityConfiguration securityConfiguration)
            : base(secureDatabase, corrector, claims, securityConfiguration)
        {
        }
    }

    [Export(typeof(IRoutedApplication))]
    internal class ArribaTableRoutesApplicationExport : ArribaTableRoutesApplication
    {
        [ImportingConstructor]
        public ArribaTableRoutesApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth, IArribaManagementService managementService, ISecurityConfiguration securityConfiguration)
            : base(f, auth, managementService, securityConfiguration)
        {
        }
    }

    [Export(typeof(IRoutedApplication))]
    internal class ArribaQueryApplicationExport : ArribaQueryApplication
    {
        [ImportingConstructor]
        public ArribaQueryApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth, ISecurityConfiguration securityConfiguration)
            : base(f, auth, securityConfiguration)
        { }
    }

    [Export(typeof(IObjectCacheFactory))]
    internal class ObjectCacheFactory : MemoryCacheFactory
    {
    }

    [Export(typeof(IApplication))]
    internal class RoutedApplicationHandlerExport : RoutedApplicationHandler
    {
        [ImportingConstructor]
        public RoutedApplicationHandlerExport([ImportMany] IEnumerable<IRoutedApplication> routes) :
            base(routes)
        { }
    }

    [Export(typeof(SecureDatabase)), Shared]
    internal class SecureDatabaseExport : SecureDatabase
    {
    }

    [Export(typeof(DatabaseFactory)), Shared]
    internal class DatabaseFactoryExport : DatabaseFactory
    {
    }

    [Export(typeof(ICorrector)), Shared]
    public class ComposedCorrectorExport : ComposedCorrector
    {
        [ImportingConstructor]
        public ComposedCorrectorExport() : base(new TodayCorrector())
        {
        }
    }
}
