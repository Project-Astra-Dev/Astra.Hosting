using Astra.Hosting.Http.Interfaces;
using Astra.Hosting.Http;
using Astra.Hosting.WebSockets;
using Astra.Hosting.WebSockets.Attributes;
using Astra.Hosting.WebSockets.Interfaces;

using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Astra.Hosting.Http.Actions;
using Astra.Hosting.Http.Attributes;
using Astra.Hosting.Http.Binding;
using Astra.Hosting.SDK;
using System.Security.Cryptography;
using System.Buffers;
using Serilog.Core;

namespace Astra.Hosting.WebSockets
{
    public abstract class AstraWebSocketServer : IWebSocketServer, IStartStopObject
    {
        private bool _initialized;
        private HttpListener _httpListener;
        private readonly List<AstraWebSocketNegotiationRoute> _negotiationRoutes;
        private readonly List<AstraWebSocketRoute> _routes;
        private Task _listenTask = null!;

        private readonly ILogger _logger;
        public ILogger Logger => _logger;

        public AstraWebSocketServer(string hostname, ushort port)
        {
            _logger = ModuleInitialization.InitializeLogger(GetType().Name);

            Hostname = hostname;
            Port = port;
            
            _routes = new List<AstraWebSocketRoute>();
            _negotiationRoutes = new List<AstraWebSocketNegotiationRoute>();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(string.Format("http://{0}:{1}/", Hostname, Port));

            Start();
        }

        public void Start()
        {
            if (!_initialized)
            {
                var negotiationRouteMethods = GetType().GetAllMethodsWithAttribute<WebSocketNegotiationRouteAttribute>();
                foreach (var negotiationRouteMethodInfo in negotiationRouteMethods)
                {
                    var socketNegotiationEndpointAttr = negotiationRouteMethodInfo.GetCustomAttribute<WebSocketNegotiationRouteAttribute>()
                        ?? throw new InvalidOperationException();

                    _negotiationRoutes.Add(new AstraWebSocketNegotiationRoute
                    {
                        EndpointName = negotiationRouteMethodInfo.Name,
                        RouteUri = string.Format("/{0}/negotiate", socketNegotiationEndpointAttr.Uri.Trim('/')),
                        MethodInfo = negotiationRouteMethodInfo,
                        Processors = negotiationRouteMethodInfo.GetCustomAttributes<HttpProcessorAttribute>().ToList(),
                    });
                }
                
                var routeMethods = GetType().GetAllMethodsWithAttribute<WebSocketRouteAttribute>();
                foreach (var routeMethodInfo in routeMethods)
                {
                    var socketEndpointAttr = routeMethodInfo.GetCustomAttribute<WebSocketRouteAttribute>()
                        ?? throw new InvalidOperationException();

                    _routes.Add(new AstraWebSocketRoute
                    {
                        EndpointName = routeMethodInfo.Name,
                        RouteUri = string.Format("/{0}", socketEndpointAttr.Uri.Trim('/')),
                        Processors = routeMethodInfo.GetCustomAttributes<WebSocketRouteProcessorAttribute>().ToList(),
                        MethodInfo = routeMethodInfo
                    });
                }

                _initialized = true;
            }

            if (!_httpListener.IsListening)
            {
                _httpListener.Start();
                _listenTask = Task.Factory.StartNew(ListenImpl, TaskCreationOptions.LongRunning);
                _logger.Information("Started socket server on '{Host}:{Port}'", Hostname, Port);
            }
            else _logger.Warning("Cannot start socket server when it is already listening!");
        }
        
        private async Task<IHttpActionResult> ProcessNegotiationRequest(IHttpContext context)
        {
            var endpoint = FindExactMatchingEndpoint(context.Request);
            if (endpoint == null) return Results.NotFound();
            
            try
            {
                foreach (var processor in endpoint.Processors)
                    if (!await processor.Validate(context))
                        return Results.ExpectationFailed(string.Format("The processor '{0}' rejected your request.", processor.GetType().GetSafeName()));
                
                var args = await BindableExtensions.BindParameters(endpoint.MethodInfo, context);
                
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
                _logger.Error("Error processing request: {Message}", ex);
                return Results.HtmlDocument(
                    HttpStatusCode.InternalServerError,
                    HtmlDocumentCache.InternalServerErrorDocumentString.Replace("{0}", string.Format("Error while processing {0}<br><br>{1}", endpoint.EndpointName, ex.ToString()))
                );
            }
        }

        private async Task DoNegotiationRouteAsync(HttpListenerContext rawHttpContext)
        {
            var context = AstraHttpContext.New(rawHttpContext);
            lock (LogContext.logLock)
            {
                _logger.Information("{IpAddress} {HttpMethod} {Uri}", context.Request.Remote, context.Request.Method, rawHttpContext.Request.Url!.PathAndQuery);
                if ((context.Request.Method == HttpMethod.Post || context.Request.Method == HttpMethod.Put) && context.Request.Body.Length > 0)
                {
                    _logger.Information("Content-Type: {ContentType}", context.Request.Headers["Content-Type"]);
                    if (context.Request.Headers["Content-Type"] == "application/json")
                    {
                        _logger.Information("\tJson Body:", context.Request.JsonBody);
                        foreach (var kvp in context.Request.JsonBody)
                            _logger.Information("\t\t{Key}: {Value}", kvp.Key, kvp.Value);
                    }
                    else if (context.Request.Headers["Content-Type"] == "application/x-www-form-urlencoded")
                    {
                        _logger.Information("\tForm Body:", context.Request.FormBody);
                        foreach (var kvp in context.Request.FormBody)
                            _logger.Information("\t\t{Key}: {Value}", kvp.Key, kvp.Value);
                    }
                }
            }
            
            IHttpActionResult actionResult;
            using (var scopedReference = ScopedReference<IHttpContext>.New(ref context))
            {
                scopedReference.SetValue(context);
                actionResult = await ProcessNegotiationRequest(context);
            }

            context.Response.ApplyToHttpListenerResponse(actionResult);
        }

        private async Task DoSocketRouteAsync(HttpListenerContext rawHttpContext)
        {
            var route = FindMatchingRoute(rawHttpContext.Request.Url!.AbsolutePath);
            if (route == null)
            {
                _logger.Error("The route was not found when connecting via WebSocket");
                rawHttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                rawHttpContext.Response.Close();
            }

            var rawWebSocketContext = await rawHttpContext.AcceptWebSocketAsync(null);
            if (rawWebSocketContext == null)
            {
                _logger.Error("The WebSocket context was not found.");
                rawHttpContext.Response.StatusCode = (int)HttpStatusCode.FailedDependency;
                rawHttpContext.Response.Close();
                return;
            }

            var result = route.MethodInfo.Invoke(this, new object[0]);
            if (result == null || result is not IWebSocketStateMachine)
            {
                _logger.Error("The method '{MethodName}' did not return a '{TypeName}'.",
                    route.EndpointName, nameof(IWebSocketStateMachine));
                rawHttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                rawHttpContext.Response.Close();
                return;
            }

            var webSocketClient = AstraWebSocketClient.New(rawHttpContext, rawWebSocketContext);
            foreach (var processor in route.Processors)
            {
                if (!await processor.Validate(webSocketClient))
                {
                    _logger.Error("The processor '{ProcessorName}' did not validate the request.",
                        route.EndpointName);
                    rawHttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    rawHttpContext.Response.Close();
                    return;
                }
            }

            _ = Task.Factory.StartNew(
                async () => await SocketImpl(webSocketClient, (AstraWebSocketStateMachine)result),
                TaskCreationOptions.LongRunning);
        }

        private async Task ListenImpl()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    var rawHttpContext = await _httpListener.GetContextAsync();
                    _logger.Information("{IpAddress} {HttpMethod} {Uri}", 
                        rawHttpContext.Request.RemoteEndPoint.Address, 
                        rawHttpContext.Request.HttpMethod, 
                        rawHttpContext.Request.Url!.PathAndQuery);

                    if (rawHttpContext.Request.IsWebSocketRequest)
                        await DoSocketRouteAsync(rawHttpContext);
                    else await DoNegotiationRouteAsync(rawHttpContext);
                }
                catch (Exception ex)
                {
                    _logger.Error("Error processing request: {Message}", ex.Message);
                }

                await Task.Delay(1);
            }
        }

        private AstraWebSocketRoute? FindMatchingRoute(string route)
        {
            return _routes.FirstOrDefault(e => e!.RouteUri == route, null);
        }
        
        private AstraWebSocketNegotiationRoute? FindExactMatchingEndpoint(IHttpRequest request)
        {
            return _negotiationRoutes.FirstOrDefault(e =>  
                string.Equals(
                    NormalizePath(e.RouteUri), 
                    NormalizePath(request.Uri), 
                    StringComparison.OrdinalIgnoreCase
                ));
        }

        private string NormalizePath(string path) =>  path.TrimEnd('/').ToLowerInvariant();

        private async Task SocketImpl(IWebSocketClient webSocketClient, AstraWebSocketStateMachine webSocketStateMachine)
        {
            try
            {
                const int MAX_BUFFER = 1024 * 8 * 4 * 2;
                webSocketStateMachine.InvokeOnOpen(webSocketClient);

                while (webSocketClient.IsConnected)
                {
                    byte[]? rawMessageBuffer = null;
                    try
                    {
                        rawMessageBuffer = ArrayPool<byte>.Shared.Rent(MAX_BUFFER);
                        int totalBytesReceived = 0;
                        bool endOfMessage = false;

                        while (!endOfMessage && webSocketClient.IsConnected)
                        {
                            var receiveResult = await webSocketClient.ReadMessageAsync(rawMessageBuffer.AsMemory(totalBytesReceived, MAX_BUFFER - totalBytesReceived));
                            totalBytesReceived += receiveResult.Count;
                            endOfMessage = receiveResult.EndOfMessage;

                            if (totalBytesReceived >= MAX_BUFFER)
                            {
                                _logger.Warning("Message exceeds maximum buffer size and may be truncated.");
                                break;
                            }
                        }

                        if (totalBytesReceived > 0)
                        {
                            byte[] message = rawMessageBuffer[..totalBytesReceived];
                            webSocketStateMachine.InvokeOnMessage(webSocketClient, message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Failed to invoke state / read buffer: {Message}", ex.Message);
                    }
                    finally
                    {
                        if (rawMessageBuffer != null) ArrayPool<byte>.Shared.Return(rawMessageBuffer);
                    }
                }

                webSocketStateMachine.InvokeOnClose(webSocketClient);
            }
            catch (Exception ex)
            {
                Log.Error("Error while handling socket: {Message}", ex.Message);
            }
        }

        public void Stop()
        {
            if (!_initialized)
                return;

            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
                _listenTask = null!;
                _logger.Information("Stopped socket server");
            }
            else _logger.Warning("Cannot stop socket server when it is not listening!");
        }


        public string Hostname { get; } = "localhost";
        public ushort Port { get; } = 80;
    }
}
