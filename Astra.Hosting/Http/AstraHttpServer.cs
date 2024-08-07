using Astra.Hosting.Http.Actions;
using Astra.Hosting.Http.Attributes;
using Astra.Hosting.Http.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Astra.Hosting.Http
{
    public abstract class AstraHttpServer : IHttpServer, IStartStopObject
    {
        private bool _initialized;
        private HttpListener _httpListener;
        private IHttpSessionProcessor? _sessionProcessor = null!;
        private readonly List<AstraHttpEndpoint> _endpoints;
        private Task _listenTask = null!;

        public AstraHttpServer(string hostname, ushort port)
        {
            Hostname = hostname;
            Port = port;
            _endpoints = new List<AstraHttpEndpoint>();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(string.Format("http://{0}:{1}/", Hostname, Port));

            Start();
        }

        public void Start()
        {
            if (!_initialized)
            {
                var endpointMethods = GetType().GetAllMethodsWithAttribute<HttpEndpointAttribute>();
                foreach (var endpointMethodInfo in endpointMethods)
                {
                    var httpEndpointAttr = endpointMethodInfo.GetCustomAttribute<HttpEndpointAttribute>()
                        ?? throw new InvalidOperationException();

                    _endpoints.Add(new AstraHttpEndpoint
                    {
                        Method = httpEndpointAttr.Method,
                        EndpointName = endpointMethodInfo.Name,
                        RouteUri = httpEndpointAttr.Uri,
                        Processors = endpointMethodInfo.GetCustomAttributes<HttpProcessorAttribute>().ToList(),
                        MethodInfo = endpointMethodInfo
                    });
                }

                _sessionProcessor = (HttpSessionProcessorAttribute?)GetType().GetCustomAttribute(typeof(HttpSessionProcessorAttribute), true)
                    ?? null;

                _initialized = true;
            }

            if (!_httpListener.IsListening)
            {
                _httpListener.Start();
                _listenTask = Task.Factory.StartNew(ListenImpl, TaskCreationOptions.LongRunning);
                Log.Information("[{Name}] Started HTTP server on '{Host}:{Port}'", GetType().GetSafeName(), Hostname, Port);
            }
            else Log.Warning("Cannot start HTTP server when it is already listening!");
        }

        private async Task ListenImpl()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    var rawHttpContext = await _httpListener.GetContextAsync();
                    var context = AstraHttpContext.New(rawHttpContext);

                    Log.Information("[{Name}] {IpAddress} {HttpMethod} {Uri}", GetType().GetSafeName(), context.Request.Remote, context.Request.Method, context.Request.Uri);

                    if (_sessionProcessor != null)
                        await _sessionProcessor.TryValidateSession(context);

                    var actionResult = await ProcessRequest(context);
                    context.Response.StatusCode = actionResult.StatusCode;
                    context.Response.ContentType = actionResult.ContentType;
                    context.Response.Content = actionResult.Body;

                    context.Response.ApplyToHttpListenerResponse();
                    rawHttpContext.Response.Close();
                }
                catch (Exception ex)
                {
                    Log.Error("Error processing request: {Message}", ex.Message);
                }

                await Task.Delay(1);
            }
        }

        private async Task<IHttpActionResult> ProcessRequest(IHttpContext context)
        {
            var endpoint = FindMatchingEndpoint(context.Request);

            if (endpoint == null) 
                return Results.NotFound();
            if (endpoint.Method != context.Request.Method)
                return Results.MethodNotAllowed();

            try
            {
                foreach (var processor in endpoint.Processors)
                    if (!await processor.Validate(context))
                        return Results.ExpectationFailed(string.Format("The processor '{0}' rejected your request.", processor.GetType().GetSafeName()));
                var args = new object[endpoint.MethodInfo.GetParameters().Length];

                if (IsDynamicRoute(endpoint.RouteUri))
                {
                    var routeParams = ExtractRouteParameters(endpoint.RouteUri, context.Request.Uri);

                    var parameters = endpoint.MethodInfo.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.ParameterType == typeof(IHttpContext))
                            args[i] = context;
                        else if (routeParams.ContainsKey(param.Name!))
                            args[i] = Convert.ChangeType(routeParams[param.Name!], param.ParameterType);
                    }
                }
                else
                {
                    var parameters = endpoint.MethodInfo.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.ParameterType == typeof(IHttpContext))
                            args[i] = context;
                    }
                }

                var result = (Task<IHttpActionResult>)endpoint.MethodInfo.Invoke(this, args)!;
                if (result is Task<IHttpActionResult> resultTask && resultTask != null)
                {
                    await resultTask;
                    return resultTask.Result;
                }
                if (result is IHttpActionResult actionResult && actionResult != null)
                    return actionResult;

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing endpoint {EndpointName}", endpoint.EndpointName);
                return Results.InternalServerError(ex.Message);
            }
        }

        private bool IsDynamicRoute(string routeUri) => routeUri.Contains("{") && routeUri.Contains("}");

        private AstraHttpEndpoint? FindMatchingEndpoint(IHttpRequest request)
        {
            return _endpoints.FirstOrDefault(e => IsRouteMatch(e.RouteUri, request.Uri), null);
        }

        private bool IsRouteMatch(string routePattern, string requestPath)
        {
            var pattern = "^" + Regex.Escape(routePattern)
                .Replace("\\{", "{")
                .Replace("}", "}")
                .Replace("{*}", "(.*)")
                .Replace("{", "(?<$1>[^/]+)") + "$";

            return Regex.IsMatch(requestPath, pattern);
        }

        private Dictionary<string, string> ExtractRouteParameters(string routePattern, string requestPath)
        {
            var pattern = "^" + Regex.Escape(routePattern)
                .Replace("\\{", "{")
                .Replace("}", "}")
                .Replace("{*}", "(.*)")
                .Replace("{", "(?<$1>[^/]+)") + "$";

            var match = Regex.Match(requestPath, pattern);
            var result = new Dictionary<string, string>();

            if (match.Success)
            {
                var groupNames = Regex.Matches(routePattern, "{([^}]+)}")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value);

                foreach (var name in groupNames)
                    result[name] = match.Groups[name].Value;
            }

            return result;
        }

        public void Stop()
        {
            if (!_initialized)
                return;

            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
                _listenTask = null!;
                Log.Information("[{Name}] Stopped HTTP server", GetType().GetSafeName());
            }
            else Log.Warning("Cannot stop HTTP server when it is not listening!");
        }

        public string Hostname { get; } = "localhost";
        public ushort Port { get; } = 80;
        public IReadOnlyList<IHttpEndpoint> Endpoints => _endpoints;
    }
}
