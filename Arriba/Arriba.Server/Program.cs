// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Monitoring;
using Arriba.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;
using AspNetHost = Microsoft.Extensions.Hosting.Host;

namespace Arriba.Server
{
    internal class Program
    {
        private const int DefaultPort = 42784;

        private static void Main(string[] args)
        {
            Console.WriteLine("Arriba Local Server\r\n");

            Configuration c = Configuration.GetConfigurationForArgs(args);
            int portNumber = c.GetConfigurationInt("port", DefaultPort);

            // Write trace messages to console if /trace is specified 
            if (c.GetConfigurationBool("trace", Debugger.IsAttached))
            {
                EventPublisher.AddConsumer(new ConsoleEventConsumer());
            }

            // Always log to CSV
            EventPublisher.AddConsumer(new CsvEventConsumer());

            CreateHostBuilder(args).Build().Run();

            Console.WriteLine("Exiting.");
            Environment.Exit(0);
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return AspNetHost.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }

        private class Startup
        {
            // This method gets called by the runtime. Use this method to add services to the container.
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
            public void ConfigureServices(IServiceCollection services)
            {
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                var host = new Arriba.Server.Hosting.Host();
                host.Add<JsonConverter>(new StringEnumConverter());
                host.Compose();

                var server = host.GetService<ComposedApplicationServer>();

                app.Run(async context =>
                {
                    server.HandleAsync(context.R)
                })
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/", async context =>
                    {
                        await context.Response.WriteAsync("Hello World!");
                    });
                });
            }
        }
    }
}

