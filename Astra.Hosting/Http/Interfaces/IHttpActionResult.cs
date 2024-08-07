using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpActionResult
    {
        HttpStatusCode StatusCode { get; }
        string ContentType { get; }
        byte[] Body { get; }
    }
}
