using Arriba.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Arriba.Adapter.Observability
{
    public class Tracker : ITracker
    {
        private readonly IArribaConvert _converter;
        private string _semantic;

        public Tracker(string semantic, IArribaConvert converter)
        {
            _converter = converter;
            _semantic = semantic;
        }

        public void LogAfter(MethodInfo methodInfo, object[] args, object result)
        {
            StringBuilder sb = GetLogHeader(methodInfo, args, result);

            Console.WriteLine($"LogAfter: {sb}");
        }

        public void LogBefore(MethodInfo methodInfo, object[] args)
        {
            StringBuilder sb = GetLogHeader(methodInfo, args);

            Console.WriteLine($"LogBefore: { sb }");
        }

        private StringBuilder GetLogHeader(MethodInfo methodInfo, object[] args, object result = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Semantic: {_semantic}");
            sb.AppendLine($"Method {methodInfo.Name}");
            var parameters = methodInfo.GetParameters();

            if (parameters.Length > 0)
            {
                sb.AppendLine("Parameters:");
                int i = 0;
                foreach (var param in parameters)
                {
                    sb.AppendLine($"{param.Name}:{args[i]}");
                    i++;
                }
            }

            if (result != null)
                sb.AppendLine($"Result: {_converter.ToJson(result)}");

            return sb;
        }

        public void LogException(Exception exception, MethodInfo methodInfo = null)
        {
            Console.WriteLine($"LogException: {_semantic} { sb }");
        }
    }
}
