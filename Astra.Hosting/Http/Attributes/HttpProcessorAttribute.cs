using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Attributes
{
    public abstract class HttpProcessorAttribute : Attribute, IHttpEndpointProcessor
    {
        public abstract Task<bool> Validate(IHttpContext httpContext);
    }
}
