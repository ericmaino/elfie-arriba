using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Arriba.Diagnostics.SemanticLogging
{
    public class ArribaTelemetry : IArribaTelemetry
    {
        IDictionary<string, string> context;
        public ArribaTelemetry()
        {
            context = new Dictionary<string, string>();
            context.Add("_ctx_id_", Guid.NewGuid().ToString());
        }

        public void ProvideContext(string itemName, string itemValue)
        {
            if(context.Keys.Contains(itemName))
            {
                context[itemName] = itemValue;
            }
            else
            {
                context.Add(itemName, itemValue);
            }
        }

        public void ProvideContext(IDictionary<string, string> contextItems)
        {
            foreach(var item in contextItems)
            {
                ProvideContext(item.Key, item.Value);
            }
        }

        public void ProvideContext (Exception exception)
        {
            ProvideContext("Exception", exception.ToString());
        }

        public void ProvideContext (string info)
        {
            ProvideContext("info", "");
        }

        public void TrackInfo()
        {
            Console.WriteLine(JsonSerializer.Serialize(context));
        }

        public void TrackInfo(string itemName, string itemValue)
        {
            ProvideContext(itemName, itemValue);
            TrackInfo();
        }

        public void TrackInfo(IDictionary<string, string>contextItems)
        {
            ProvideContext(contextItems);
            TrackInfo();
        }

        public void TrackException (Exception exception)
        {
            ProvideContext(exception);
            TrackInfo();
        }

        public void TrackInfo (string info)
        {
            ProvideContext(info);
            TrackInfo();
        }
    }
}