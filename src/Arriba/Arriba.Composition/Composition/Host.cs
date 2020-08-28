// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Arriba.Caching;
using Arriba.Communication;
using Arriba.Communication.Application;
using Arriba.Communication.ContentTypes;
using Arriba.Communication.Server.Application;
using Arriba.Configuration;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.Server;
using Arriba.Server.Application;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Arriba.Composition
{
    public static class HostExtensions
    {
        public static void AddArribaServices(this IServiceCollection services, ISecurityConfiguration config)
        {
            services.AddSingleton<ISecurityConfiguration>(config);
         
            var arribaTypes = GetArribaTypes();
            services.AddDerivedTypes<IChannel>(arribaTypes);
            services.AddDerivedTypes<IContentReader>(arribaTypes);
            services.AddDerivedTypes<IContentWriter>(arribaTypes);
            services.AddDerivedTypes<JsonConverter>(arribaTypes);
            services.AddTransient<JsonContentWriter>();

            services.AddSingleton<ClaimsAuthenticationService>();
            services.AddSingleton<IArribaManagementService, ArribaManagementService>();
            services.AddSingleton<IObjectCacheFactory, MemoryCacheFactory>();
            services.AddSingleton<SecureDatabase>();
            services.AddSingleton<DatabaseFactory>();
            services.AddSingleton<ICorrector, TodayCorrector>();

            services.AddTransient<IRoutedApplication, ArribaImportApplication>();
            services.AddTransient<IRoutedApplication, ArribaQueryApplication>();
            services.AddTransient<IRoutedApplication, ArribaTableRoutesApplication>();

            services.AddTransient<IApplication, RoutedApplicationHandler>();
            services.AddSingleton<ApplicationServer, ComposedApplicationServer>();
        }

        public static void AddDerivedTypes<T>(this IServiceCollection services, IEnumerable<Type> types, ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            var serviceType = typeof(T);
            var query = types
                .Where(instanceType => !instanceType.IsInterface && !instanceType.IsAbstract)
                .Where(instanceType => serviceType.IsAssignableFrom(instanceType));

            foreach (Type t in query)
            {
                services.Add(new ServiceDescriptor(serviceType, t, lifetime));
            }
        }

        private static IEnumerable<Type> GetArribaTypes()
        {
            var assemblies = new[] {
                // Arriba.dll
                typeof(Table).Assembly, 
                // Arriba.Adapter.Newtonsoft
                typeof(JsonContentWriter).Assembly
            };

            return assemblies.SelectMany(x => x.GetTypes())
                .ToList();
        }
    }
}
