// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Arriba.Composition;
using Arriba.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arriba.Test
{
    public class ArribaServiceProvider
    {
        public static IServiceProvider CreateTestingProvider()
        {
            var factory = new DefaultServiceProviderFactory();
            var services = new ServiceCollection();
            services.AddArribaServices(new DebugSecurityConfig());
            return factory.CreateServiceProvider(services);
        }

        private class DebugSecurityConfig : ISecurityConfiguration
        {
            public bool EnabledAuthentication { get { return false; } }

            public IOAuthConfig OAuthConfig => throw new NotImplementedException();
        }
    }
}
