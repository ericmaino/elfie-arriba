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

    }
}