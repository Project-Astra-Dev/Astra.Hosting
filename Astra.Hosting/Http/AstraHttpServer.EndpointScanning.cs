using Astra.Hosting.Http.Attributes;
using Astra.Hosting.Http.Controllers;
using Astra.Hosting.Http.Controllers.Attributes;
using Astra.Hosting.Http.Controllers.Interfaces;
using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http
{
    public abstract partial class AstraHttpServer : IHttpServer, IStartStopObject
    {
        private void ScanForServerBasedEndpoints(Type? targetType = null)
        {
            var endpointMethods = (targetType ?? GetType()).GetAllMethodsWithAttribute<HttpEndpointAttribute>();
            foreach (var endpointMethodInfo in endpointMethods)
            {
                var httpEndpointAttr = endpointMethodInfo.GetCustomAttribute<HttpEndpointAttribute>()
                    ?? throw new InvalidOperationException();

                _endpoints.Add(new AstraHttpEndpoint
                {
                    Method = httpEndpointAttr.Method,
                    EndpointName = endpointMethodInfo.Name,
                    RouteUri = string.Format("/{0}", httpEndpointAttr.Uri.Trim('/')),
                    Processors = endpointMethodInfo.GetCustomAttributes<HttpProcessorAttribute>().ToList(),
                    MethodInfo = endpointMethodInfo
                });
            }
        }

        private void ScanForControllerBasedEndpoints(Type? targetType = null)
        {
            var controllerTypes = (targetType ?? GetType()).GetAllNestedTypesWithAttribute<HttpControllerAttribute>();
            foreach (var controllerType in controllerTypes)
            {
                var httpControllerAttr = controllerType.GetCustomAttribute<HttpControllerAttribute>()
                    ?? throw new InvalidOperationException();
                var controllerEndpointMethods = controllerType.GetAllMethodsWithAttribute<HttpEndpointAttribute>();

                foreach (var endpointMethodInfo in controllerEndpointMethods)
                {
                    var httpEndpointAttr = endpointMethodInfo.GetCustomAttribute<HttpEndpointAttribute>()
                        ?? throw new InvalidOperationException();

                    var logger = ModuleInitialization.InitializeLogger(controllerType.GetSafeName());

                    var controllerInstance = (AstraHttpController?)Activator.CreateInstance(controllerType)
                        ?? throw new InvalidOperationException("Could not instantiate '" + controllerType.Name + "'. Is there a problem with the constructor taking in an IHttpServer and ILogger?");
                    controllerInstance.OnCreated(this, logger);

                    _endpoints.Add(new AstraHttpEndpoint
                    {
                        Method = httpEndpointAttr.Method,
                        EndpointName = endpointMethodInfo.Name,
                        RouteUri = string.Format("/{0}/{1}", httpControllerAttr.Uri.Trim('/'), httpEndpointAttr.Uri.Trim('/')).Replace("//", "/"),
                        Processors = endpointMethodInfo.GetCustomAttributes<HttpProcessorAttribute>().ToList(),
                        MethodInfo = endpointMethodInfo,
                        ControllerInstance = controllerInstance
                    });
                }
            }
        }
    }
}
