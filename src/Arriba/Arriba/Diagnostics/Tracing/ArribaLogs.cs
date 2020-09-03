using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Arriba.Diagnostics.Tracing
{
    public class ArribaLog
    {
        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        public IDisposable EnableConsoleOutput()
        {
            var consolewriter = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(consolewriter);
            return consolewriter;
        }
    }
}