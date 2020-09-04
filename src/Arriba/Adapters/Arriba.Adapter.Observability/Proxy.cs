using Arriba.Serialization;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Arriba.Adapter.Observability
{
    public class Proxy<T> : DispatchProxy
    {
        private T _decorated;
        private ITracker _tracker;

        public static T Create(T decorated, ITracker tracker)
        {
            object proxy = Create<T, Proxy<T>>();
            ((Proxy<T>)proxy).SetParameters(decorated, tracker);

            return (T)proxy;
        }

        private void SetParameters(T decorated, ITracker tracker)
        {
            _decorated = decorated;
            _tracker = tracker;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

            _tracker.LogBefore(targetMethod, args);
            try
            {
                var result = targetMethod.Invoke(_decorated, args);
            }
            catch (Exception ex)
            {
                _tracker.LogException(ex, targetMethod);
            }

            return null;
        }
    }
}
