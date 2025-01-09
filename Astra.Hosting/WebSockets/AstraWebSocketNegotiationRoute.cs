using Astra.Hosting.Http.Interfaces;
using System.Reflection;

namespace Astra.Hosting.WebSockets;

public class AstraWebSocketNegotiationRoute
{
    public string EndpointName { get; internal set; } = "Unknown";
    public string RouteUri { get; internal set; } = "/";
    public MethodInfo MethodInfo { get; internal set; }
    public IReadOnlyList<IHttpEndpointProcessor> Processors { get; set; } = new List<IHttpEndpointProcessor>();
    

    public async Task<bool> Validate(IHttpContext httpContext)
    {
        if (Processors.Count == 0) 
            return true;

        for (int i = 0; i < Processors.Count; i++)
        {
            if (await Processors[i].Validate(httpContext))
                return false;
        }    
        return true;
    }
}