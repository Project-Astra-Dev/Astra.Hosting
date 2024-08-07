using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http
{
    public sealed class AstraHttpEndpoint : IHttpEndpoint
    {
        public HttpMethod Method { get; internal set; } = HttpMethod.Get;
        public string EndpointName { get; internal set; } = "Unknown";
        public string RouteUri { get; internal set; } = "/";
        public IReadOnlyList<IHttpEndpointProcessor> Processors { get; internal set; } = new List<IHttpEndpointProcessor>();
        public MethodInfo MethodInfo { get; internal set; }

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
}
