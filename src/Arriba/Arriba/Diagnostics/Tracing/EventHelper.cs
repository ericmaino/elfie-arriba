using System;
using System.Collections.Generic;
using System.Text;

namespace Arriba.Diagnostics.Tracing
{

    public static class EventHelper
    {
        public static void Raise(this EventHandler eventHandler, object sender, EventArgs args)
        {
            if (eventHandler == null)
                return;
            eventHandler(sender, args);
        }
    }
}
