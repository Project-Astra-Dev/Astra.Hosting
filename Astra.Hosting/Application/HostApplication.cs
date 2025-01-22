using Astra.Hosting.Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Serilog;

namespace Astra.Hosting.Application
{
    public sealed class HostApplication : IHostApplication
    {
        private static readonly ILogger _logger = ModuleInitialization.InitializeLogger("HostApplication");
        
        public static HostApplication Instance { get; private set; }

        private readonly string _name;
        private readonly ContainerBuilder _containerBuilder;
        private readonly List<Type> _allServerTypes = new List<Type>();
        private readonly List<IStartStopObject> _allServerObjects = new List<IStartStopObject>();
        private Action<IContainer>? _prepareAction;
        private readonly HostConfiguration _configuration;

        private IContainer _container = null!;
        internal static IContainer Container => Instance._container;

        private HostApplication(string name)
        {
            Instance = this;
            _configuration = HostConfiguration.FromFile("appsettings.json");
            
            _name = name;
            _containerBuilder = new ContainerBuilder();
        }
        public static IHostApplication New(string name)
            => new HostApplication(name);

        public IHostApplication ConfigureServices(Action<ContainerBuilder> onConfigureServicesAction)
        {
            onConfigureServicesAction.Invoke(_containerBuilder);
            return this;
        }

        public IHostApplication OnPrepare(Action<IContainer> onPrepareAction)
        {
            _prepareAction = onPrepareAction;
            return this;
        }

        public IHostApplication AddServer<TInterface, TServer>() 
            where TServer : class, IStartStopObject 
            where TInterface : notnull
        {
            _containerBuilder.RegisterType<TServer>()
                .As<TInterface>()
                .SingleInstance();
    
            _allServerTypes.Add(typeof(TInterface));
            return this;
        }

        public object[] PopulateArguments(MethodInfo methodInfo, object[] args)
        {
            if (args.Length == 0) return args;
            var methodParameters = methodInfo.GetParameters();

            if (methodParameters.Length < args.Length) 
                return args;

            for (var i = 0; i < args.Length; i++)
            {
                var parameter = methodParameters[i];
                var service = _container.ResolveOptional(parameter.ParameterType);
                if (service != null) args[i] = service;
            }
            return args;
        }
        
        public TInterface Get<TInterface>() where TInterface : notnull => _container.Resolve<TInterface>();
        public object Get(Type interfaceType) => _container.ResolveOptional(interfaceType);

        public async Task RunAsync()
        {
            if (_container != null) 
                throw new InvalidOperationException("Container has already been built by RunAsync().");

            {
                _containerBuilder.AddSingletonFactory<IHostConfiguration, HostConfiguration>(
                    (ctx, parameters) => _configuration                 // Initialize host configuration
                );
            }
            
            _container = _containerBuilder.Build();
            if (_container == null!)
            {
                _logger.Error("The container has not been built yet. This is probably due to a misconfiguration.");
                return;
            }

            _prepareAction?.Invoke(_container);
            foreach (var serverType in _allServerTypes)
                _allServerObjects.Add((IStartStopObject)_container.Resolve(serverType));
            await Task.Delay(-1);
        }
    }
}
