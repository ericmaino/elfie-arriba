using System;
using System.Reflection;

namespace Arriba.Adapter.Observability
{
    public interface ITracker
    {
        void LogAfter(MethodInfo methodInfo, object[] args, object result);

        void LogBefore(MethodInfo methodInfo, object[] args);

        void LogException(Exception exception, MethodInfo methodInfo = null);

    }
}