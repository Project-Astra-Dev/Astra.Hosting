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
using System.Security.Cryptography;
using System.Buffers;

namespace Astra.Hosting.WebSockets
{
    public abstract class AstraWebSocketServer : IWebSocketServer, IStartStopObject
    {
        private bool _initialized;
        private HttpListener _httpListener;
        private readonly List<AstraWebSocketRoute> _routes;
        private Task _listenTask = null!;

        public AstraWebSocketServer(string hostname, ushort port)
        {
            Hostname = hostname;
            Port = port;
            _routes = new List<AstraWebSocketRoute>();

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(string.Format("http://{0}:{1}/", Hostname, Port));

            Start();
        }

        public void Start()
        {
            if (!_initialized)
            {
                var routeMethods = GetType().GetAllMethodsWithAttribute<WebSocketRouteAttribute>();
                foreach (var routeMethodInfo in routeMethods)
                {
                    var socketEndpointAttr = routeMethodInfo.GetCustomAttribute<WebSocketRouteAttribute>()
                        ?? throw new InvalidOperationException();

                    _routes.Add(new AstraWebSocketRoute
                    {
                        EndpointName = routeMethodInfo.Name,
                        RouteUri = socketEndpointAttr.Uri,
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
                Log.Information("[{Name}] Started socket server on '{Host}:{Port}'", GetType().GetSafeName(), Hostname, Port);
            }
            else Log.Warning("Cannot start socket server when it is already listening!");
        }

        private async Task ListenImpl()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    var rawHttpContext = await _httpListener.GetContextAsync();

                    if (rawHttpContext.Request.IsWebSocketRequest)
                    {
                        var route = FindMatchingRoute(rawHttpContext.Request.Url!.AbsolutePath);

                        var rawWebSocketContext = await rawHttpContext.AcceptWebSocketAsync(null);
                        if (rawWebSocketContext == null)
                        {
                            rawHttpContext.Response.StatusCode = (int)HttpStatusCode.FailedDependency;
                            rawHttpContext.Response.Close();
                            return;
                        }

                        var result = route!.MethodInfo.Invoke(this, new object[0]);
                        if (result == null || result is not IWebSocketStateMachine)
                        {
                            rawHttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                            rawHttpContext.Response.Close();
                            return;
                        }

                        var webSocketClient = AstraWebSocketClient.New(rawHttpContext, rawWebSocketContext);
                        foreach (var processor in route.Processors)
                        {
                            if (!await processor.Validate(webSocketClient))
                            {
                                rawHttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                                rawHttpContext.Response.Close();
                                return;
                            }
                        }

                        _ = Task.Factory.StartNew(async () => await SocketImpl(webSocketClient, (AstraWebSocketStateMachine)result),
                            TaskCreationOptions.LongRunning);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error processing request: {Message}", ex.Message);
                }

                await Task.Delay(1);
            }
        }

        private AstraWebSocketRoute? FindMatchingRoute(string route)
        {
            return _routes.FirstOrDefault(e => e!.RouteUri == route, null);
        }

        private async Task SocketImpl(IWebSocketClient webSocketClient, AstraWebSocketStateMachine webSocketStateMachine)
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
                            Log.Warning("Message exceeds maximum buffer size and may be truncated.");
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
                    Log.Error("Failed to invoke state / read buffer: {Message}", ex.Message);
                }
                finally
                {
                    if (rawMessageBuffer != null) ArrayPool<byte>.Shared.Return(rawMessageBuffer);
                }
            }

            webSocketStateMachine.InvokeOnClose(webSocketClient);
        }


        public void Stop()
        {
            if (!_initialized)
                return;

            if (_httpListener.IsListening)
            {
                _httpListener.Stop();
                _listenTask = null!;
                Log.Information("[{Name}] Stopped socket server", GetType().GetSafeName());
            }
            else Log.Warning("Cannot stop socket server when it is not listening!");
        }


        public string Hostname { get; } = "localhost";
        public ushort Port { get; } = 80;
    }
}
