using Astra.Hosting.Http;
using Astra.Hosting.Http.Actions;
using Astra.Hosting.Http.Attributes;
using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Astra.Hosting.Test
{
    public sealed class HttpServer : AstraHttpServer
    {
        public HttpServer() : base("localhost", 80)
        {
        }

        [HttpGet("/"), EndpointProcessor]
        public async Task<IHttpActionResult> Index(IHttpContext httpContext)
        {
            return Results.Ok("Test t=" + httpContext.Request.GetQueryParameter("t"));
        }
    }
}
