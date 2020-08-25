using System;
using System.Threading.Tasks;
using Arriba.Diagnostics.Tracing;

namespace Arriba
{
    public class ArribaProgram
    {
        public static async Task Run<T>(Func<Task> program)
        {
            await ArribaRun<T>(program);
        }

        public static async Task<int> Run<T>(Func<Task<int>> program)
        {
            var x = new ReturnCode();

            await ArribaRun<T>(async () =>
            {
                x.Value = await program();
            });

            return x.Value;
        }

        private static async Task ArribaRun<T>(Func<Task> program)
        {
            using (ArribaLogs.EnableConsoleOutput())
            {
                try
                {
                    ArribaEventSource.Log.ServiceStart<T>();
                    await program();
                }
                finally
                {
                    ArribaEventSource.Log.ServiceComplete<T>();
                }
            }
        }

        private class ReturnCode
        {
            public int Value { get; set; }
        }
    }
}
