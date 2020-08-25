using System.Diagnostics.Tracing;

namespace Arriba.Diagnostics.Tracing
{
    public sealed class ArribaEventSource : EventSource
    {
        public static ArribaEventSource Log { get; }

        static ArribaEventSource()
        {
            Log = new ArribaEventSource();
        }

        private ArribaEventSource()
            : base(nameof(ArribaEventSource))
        {
        }

        [NonEvent]
        public void ServiceStart<T>()
        {
            ServiceStart(typeof(T).Name);
        }

        [NonEvent]
        public void ServiceComplete<T>()
        {
            ServiceExit(typeof(T).Name);
        }

        [Event(1, Level = EventLevel.Informational, Message = "Starting service {0}")]
        private void ServiceStart(string serviceName)
        {
            WriteEvent(1, serviceName);
        }

        [Event(2, Level = EventLevel.Informational, Message = "Completing service {0}")]
        private void ServiceExit(string serviceName)
        {
            WriteEvent(2, serviceName);
        }
    }
}
