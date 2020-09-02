using Arriba.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.Json;

namespace Arriba.Diagnostics.Tracing
{
    public class ArribaSymmetricEvent : EventSource, ISymmetricEvent
    {
        private bool disposedValue;
        private readonly Stopwatch sw = new Stopwatch();
        private string name = string.Empty;
        protected string eventPayload = string.Empty;

        protected void Initialize()
        {
            if (name == string.Empty) name = "SymmetricEvent";
            sw.Start();
        }

        public ArribaSymmetricEvent()
        {
            Initialize();
        }

        public ArribaSymmetricEvent(string eventName)
        {
            name = eventName;
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WriteSymmetricEvent();
                    sw.Stop();
                }

                disposedValue = true;
                base.Dispose(disposing);
            }
        }

        [Event(eventId: 1)]
        protected void WriteSymmetricEvent()
        {
            WriteEvent(1, new { Name = name, TimeElapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds), Payload = eventPayload });
        }
    }

    public class ArribaSymmetricEvent<T> : ArribaSymmetricEvent where T : ISerializable
    {
        private bool disposedValue;
        public ArribaSymmetricEvent()
        {
            Initialize();
        }

        public ArribaSymmetricEvent(T payload)
        {
            eventPayload = JsonSerializer.Serialize<T>(payload);
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                base.Dispose(disposing);
            }
        }
    }
}
