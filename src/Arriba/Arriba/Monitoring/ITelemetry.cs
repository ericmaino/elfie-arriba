﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Arriba.Monitoring
{
    public interface ITelemetry
    {
        /// <summary>
        /// Begins a monitoring block for the request context. 
        /// </summary>
        /// <param name="level">Event Level</param>
        /// <param name="name">Name of the timing block.</param>
        /// <param name="detail">Detail message for event.</param>
        /// <returns>A disposable handle.</returns>
        IDisposable Monitor(MonitorEventLevel level, string name, string type = null, string identity = null, object detail = null);

        IList<MonitorEventScope> MonitorEvents();
    }
}
