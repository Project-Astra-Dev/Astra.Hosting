using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.WebSockets.Attributes
{
    public sealed class WebSocketRouteAttribute : Attribute
    {
        public WebSocketRouteAttribute(string uri = "/")
        {
            Uri = uri;
        }

        public string Uri { get; } = "/";
    }
}
