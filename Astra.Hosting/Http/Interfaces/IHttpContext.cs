using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpContext
    {
        IHttpRequest Request { get; }
        IHttpResponse Response { get; }
        IHttpSession Session { get; }
    }
}
