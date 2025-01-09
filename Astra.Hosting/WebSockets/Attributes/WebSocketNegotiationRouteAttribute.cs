using Astra.Hosting.Http.Interfaces;
using System.Net;

namespace Astra.Hosting.WebSockets.Attributes;

public sealed class WebSocketNegotiationRouteAttribute : Attribute
{
    public WebSocketNegotiationRouteAttribute(string uri = "/")
    {
        Uri = uri;
    }

    public string Uri { get; } = "/";
}