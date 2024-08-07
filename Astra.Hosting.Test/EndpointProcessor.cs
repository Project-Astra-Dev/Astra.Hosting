using Astra.Hosting.Http.Attributes;
using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Test
{
    public sealed class EndpointProcessor : HttpProcessorAttribute
    {
        public override async Task<bool> Validate(IHttpContext httpContext)
        {
            if (httpContext == null) return false;
            return httpContext.Request.Queries.ContainsKey("t");
        }
    }
}
