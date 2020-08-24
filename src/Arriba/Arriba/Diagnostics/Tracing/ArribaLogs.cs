using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Arriba.Diagnostics.Tracing
{
    public static class ArribaLogs
    {
        public static IDisposable EnableConsoleOutput()
        {
            var consolewriter = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(consolewriter);
            return consolewriter;
        }

    }
}
