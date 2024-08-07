using Astra.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Application
{
    public sealed class HostApplication : IHostApplication
    {
        private readonly string _name;
        private readonly IServiceHost _serviceHost;
        private ServiceProvider _serviceProvider = null!;

        private HostApplication(string name)
        {
            _name = name;
            _serviceHost = ServiceHost.Create(name);
        }
        public static IHostApplication New(string name)
            => new HostApplication(name);

        public IHostApplication ConfigureServices(Action<IServiceBuilder> onConfigureServicesAction)
        {
            _serviceHost.AddDependencies(onConfigureServicesAction);
            return this;
        }

        public IHostApplication AddServer<TServer, TInterface>() where TServer : class, IStartStopObject
        {
            _serviceHost.AddDependencies(options => options.AddSingleton<TServer, TInterface>());
            return this;
        }

        public async Task RunAsync()
        {
            _serviceProvider = _serviceHost.GetServiceProvider();
            await Task.Delay(-1);
        }
    }
}
