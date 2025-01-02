﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpEndpoint
    {
        Task<bool> Validate(IHttpContext httpContext);

        HttpMethod Method { get; }
        string EndpointName { get; }
        string RouteUri { get; }
        IReadOnlyList<IHttpEndpointProcessor> Processors { get; }
        MethodInfo MethodInfo { get; }
    }
}
