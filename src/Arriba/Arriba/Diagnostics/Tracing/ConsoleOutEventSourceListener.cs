using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace Arriba.Diagnostics.Tracing
{
    public class ConsoleOutEventSourceListener : EventListener
    {
        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            Console.WriteLine(FormatEvent(e));
        }

        private string FormatEvent(EventWrittenEventArgs e)
        {
            var output = new StringBuilder();
            var logTime = DateTimeOffset.Now;

            output.Append(logTime.ToString("HH:mm:ss"));
            output.Append(" - ");

            if (!string.IsNullOrEmpty(e.Message))
            {
                output.AppendFormat(e.Message, e.Payload.ToArray());
            } else
            {
                output.Append(e.EventName);
            }

            output.Append(" - ");
            output.Append(e.Level);

            if (FirstPayloadIsSerializedException(e))
            {
                output.AppendLine();
                output.Append(e.Payload.First());
            }
            
            return output.ToString();
        }

        private bool FirstPayloadIsSerializedException(EventWrittenEventArgs e)
        {
            return string.Equals(e.PayloadNames.FirstOrDefault(), "Exception", StringComparison.OrdinalIgnoreCase);
        }
    }
}
