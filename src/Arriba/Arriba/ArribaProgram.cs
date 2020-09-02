using System;
using System.Threading.Tasks;
using Arriba.Diagnostics;
using Arriba.Diagnostics.Tracing;

namespace Arriba
{
    public class ArribaProgram
    {
        private readonly ILoggingContext _log;

        public ArribaProgram()
            : this(LoggingContextFactory.CreateDefaultLoggingContext())
        {
        }

        public ArribaProgram(ILoggingContext log)
        {
            _log = log;
        }

        public async Task Run<T>(Func<Task> program)
        {
            await ArribaRun<T>(program);
        }

        public async Task<int> Run<T>(Func<Task<int>> program)
        {
            var x = new ReturnCode();

            await ArribaRun<T>(async () =>
            {
                x.Value = await program();
            });

            return x.Value;
        }

        private async Task ArribaRun<T>(Func<Task> program)
        {
                try
                {
                    _log.ServiceStart<T>();
                    await program();
                }
                catch (Exception ex)
                {
                    _log.TrackFatalException(ex, null);
                }
                finally
                {
                    _log.ServiceComplete<T>();
                }
        }

        private class ReturnCode
        {
            public int Value { get; set; }
        }
    }
}