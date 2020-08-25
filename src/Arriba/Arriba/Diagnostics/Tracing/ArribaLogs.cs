using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Arriba.Diagnostics.Tracing
{
    public static class ArribaLogs
    {
        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void WriteLine(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        public static IDisposable EnableConsoleOutput()
        {
            var consolewriter = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(consolewriter);
            return consolewriter;
        }

    }
}
