using System;
using System.Collections.Generic;

namespace Arriba.Monitoring
{
    public class Telemetry : ITelemetry
    {
        private List<MonitorEventScope> _events = new List<MonitorEventScope>();
        private EventPublisherSource _eventSource;

        public Telemetry(MonitorEventLevel level, string source, string detail)
        {
            MonitorEventEntry defaults = new MonitorEventEntry()
            {
                Level = level,
                Source = source,
                Detail = detail
            };

            _eventSource = EventPublisher.CreateEventSource(defaults);
        }

        public IDisposable Monitor(MonitorEventLevel level, string name, string type = null, string identity = null, object detail = null)
        {
            string detailValue = null;

            if (detail != null)
            {
                detailValue = ArribaConvert.ToJson(detail);
            }

            var evt = _eventSource.RaiseScope(level, type, identity, name, detailValue);
            _events.Add(evt);
            return evt;
        }

        public IList<MonitorEventScope> MonitorEvents() => _events;
    }
}
