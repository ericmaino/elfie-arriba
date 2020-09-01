using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.Json;

namespace Arriba.Diagnostics.Tracing
{
    class ArribaSymmetricEvent<T> : EventSource, ISymmetricEvent where T: ISerializable
    {
        private bool disposedValue;
        private readonly Stopwatch sw = new Stopwatch();
        private string name = string.Empty;
        private string eventPayload = string.Empty;

        private void Initialize()
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

        public ArribaSymmetricEvent(T payload)
        {
            eventPayload = JsonSerializer.Serialize<T>(payload);
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    base.Dispose(disposing);
                    WriteSymmetricEvent();
                    sw.Stop();
                }

                disposedValue = true;
                base.Dispose(disposing);
            }
        }

        [Event(eventId:1)]
        void WriteSymmetricEvent()
        {
            WriteEvent(1, new { Name = name, TimeElapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds), Payload = eventPayload });
        }
    }
}
