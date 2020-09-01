using System;
using System.Collections.Generic;

namespace Arriba.Diagnostics.SemanticLogging
{
    public interface IArribaTelemetry
    {
        void TrackInfo();

        void TrackInfo(string info);

        void TrackInfo(string itemName, string itemValue);

        void TrackInfo(IDictionary<string, string> contextItems);

        void TrackException(Exception exception);

        void ProvideContext(string itemName, string itemValue);
        void ProvideContext(IDictionary<string, string> contextItem);
    }
}
