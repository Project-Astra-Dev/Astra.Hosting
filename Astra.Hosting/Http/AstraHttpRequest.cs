using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Web;

namespace Astra.Hosting.Http
{
    public sealed class AstraHttpRequest : IHttpRequest
    {
        private readonly HttpListenerRequest _request;

        private byte[]? _body;
        private Dictionary<string, object>? _jsonBody;
        private Dictionary<string, string>? _formBody;

        private AstraHttpRequest(HttpListenerRequest request)
        {
            _request = request;
        }

        public static IHttpRequest New(HttpListenerRequest httpListenerRequest)
            => new AstraHttpRequest(httpListenerRequest);

        public string? GetHeaderValue(string key, [Optional] string? defaultValue) => Headers.TryGetValue(key, out var value) ? value : defaultValue;
        public string? GetQueryParameter(string key, [Optional] string? defaultValue) => Queries.TryGetValue(key, out var value) ? value : defaultValue;

        private HttpMethod _method;
        public HttpMethod Method
        {
            get
            {
                if (_method == null)
                    _method = new HttpMethod(_request.HttpMethod);
                return _method;
            }
        }

        public string Host => _request.Url.Host;
        public string RequestId => _request.RequestTraceIdentifier.ToString();
        public string Uri => _request.Url.AbsolutePath;

        public Dictionary<string, string> Headers => _request.Headers.AllKeys.ToDictionary(k => k!, k => _request.Headers[k])!;
        public Dictionary<string, string> Queries => _request.QueryString.AllKeys.ToDictionary(k => k!, k => _request.QueryString[k])!;

        public byte[] Body
        {
            get
            {
                if (_body == null)
                {
                    using var ms = new MemoryStream();
                    _request.InputStream.CopyTo(ms);
                    _body = ms.ToArray();
                }
                return _body;
            }
        }

        public Dictionary<string, object> JsonBody
        {
            get
            {
                if (_jsonBody == null)
                {
                    if (_request.ContentType!.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        _jsonBody = JsonSerializer.Deserialize<Dictionary<string, object>>(Body) ?? new Dictionary<string, object>();
                    }
                    else throw new InvalidOperationException("The request body is not in JSON format.");
                }
                return _jsonBody;
            }
        }

        public Dictionary<string, string> FormBody
        {
            get
            {
                if (_formBody == null)
                {
                    if (_request.ContentType!.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                    {
                        _formBody = HttpUtility.ParseQueryString(Encoding.UTF8.GetString(Body)).AllKeys
                                               .ToDictionary(k => k!, k => HttpUtility.ParseQueryString(Encoding.UTF8.GetString(Body))[k])!;
                    }
                    else throw new InvalidOperationException("The request body is not in URL-encoded format.");
                }
                return _formBody;
            }
        }

        public IPAddress Remote
        {
            get
            {
                if (Headers.ContainsKey("Cf-Connecting-Ip")) return IPAddress.Parse(Headers["Cf-Connecting-Ip"]);
                if (Headers.ContainsKey("X-Real-Ip")) return IPAddress.Parse(Headers["X-Real-Ip"]);

                return _request.RemoteEndPoint.Address;
            }
        }

        public IPAddress Origin => _request.LocalEndPoint.Address;
    }
}