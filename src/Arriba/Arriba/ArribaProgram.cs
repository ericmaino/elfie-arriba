using System;
using System.Threading.Tasks;
using Arriba.Diagnostics.Tracing;

namespace Arriba
{
    public class ArribaProgram
    {
        public static async Task Run<T>(Func<Task> program)
        {
            await program();
        }

        public static async Task<int> Run<T>(Func<Task<int>> program)
        {
            var x = new ReturnCode();

            await Run<T>(async () =>
            {
                x.Value = await program();
            });

            return x.Value;
        }

        private class ReturnCode
        {
            public int Value { get; set; }
        }
    }
}
