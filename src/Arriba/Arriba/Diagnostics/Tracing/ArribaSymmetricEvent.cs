using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;

namespace Arriba.Diagnostics.Tracing
{
    class ArribaSymmetricEvent : EventSource, ISymmetricEvent
    {
        private bool disposedValue;
        private readonly Stopwatch sw = new Stopwatch();
        private readonly string name = "SymmetricEvent";

        public ArribaSymmetricEvent()
        {
            sw.Start();
        }

        public ArribaSymmetricEvent(string name)
        {
            this.name = name;
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
            WriteEvent(1, new { Name = name, TimeElapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds) });
        }
    }
}
