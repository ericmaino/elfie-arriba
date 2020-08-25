// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Arriba.Configuration;
using Arriba.Diagnostics.Tracing;
using Arriba.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using AspNetHost = Microsoft.Extensions.Hosting.Host;

namespace Arriba.Server
{
    internal class ArribaServer
    {
        private static async Task Main(string[] args)
        {
            await ArribaProgram.Run<ArribaServer>(() =>
            {
                ArribaLogs.WriteLine("Arriba Local Server\r\n");

                var configLoader = new ArribaConfigurationLoader(args);

                // Write trace messages to console if /trace is specified 
                if (configLoader.GetBoolValue("trace", Debugger.IsAttached))
                {
                    EventPublisher.AddConsumer(new ConsoleEventConsumer());
                }

                // Always log to CSV
                EventPublisher.AddConsumer(new CsvEventConsumer());

                Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

                CreateHostBuilder(args).Build().Run();

                ArribaLogs.WriteLine("Exiting.");
                Environment.Exit(0);
                return Task.CompletedTask;
            });
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return AspNetHost.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}

