// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Arriba.Communication;
using Arriba.Diagnostics.SemanticLogging;

namespace Arriba.Composition
{
    /// <summary>
    /// Represents an application server that fulfills its dependnecies via composition.
    /// </summary>
    public class ComposedApplicationServer : ApplicationServer
    {
        public ComposedApplicationServer(
            IEnumerable<IApplication> applications,
            IEnumerable<IContentReader> readers,
            IEnumerable<IContentWriter> writers,
            IEnumerable<IChannel> channels,
            IArribaTelemetry telemetry) : base(telemetry)
        {
            if (!applications.Any())
            {
                throw new ArgumentException("No applications registered");
            }

            //if(!channels.Any())
            //{
            //    throw new ArgumentException("No channels registered");
            //}

            foreach (var application in applications)
            {
                this.RegisterApplication(application);
            }

            foreach (var reader in readers)
            {
                this.RegisterContentReader(reader);
            }

            foreach (var writer in writers)
            {
                this.RegisterContentWriter(writer);
            }

            foreach (var channel in channels)
            {
                this.RegisterRequestChannel(channel);
            }
        }
    }
}
