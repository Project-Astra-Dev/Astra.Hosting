using Astra.Hosting.WebSockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.WebSockets.Attributes
{
    public abstract class WebSocketRouteProcessorAttribute : Attribute, IWebSocketRouteProcessor
    {
        public abstract Task<bool> Validate(IWebSocketClient socketClient);
    }
}
