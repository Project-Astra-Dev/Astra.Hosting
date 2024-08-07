using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Interfaces
{
    public interface IHttpRequest
    {
        string? GetQueryParameter(string key, [Optional] string? defaultValue);
        string? GetHeaderValue(string key, [Optional] string? defaultValue);

        HttpMethod Method { get; }
        string Host { get; }
        string RequestId { get; }
        string Uri { get; }

        Dictionary<string, string> Headers { get; }
        Dictionary<string, string> Queries { get; }

        byte[] Body { get; }

        IPAddress Remote { get; }
        IPAddress Origin { get; }
    }
}
