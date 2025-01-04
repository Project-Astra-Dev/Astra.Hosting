using System.Reflection;
using Autofac;

namespace Astra.Hosting.Application
{
    public interface IHostApplication
    {
        IHostApplication ConfigureServices(Action<ContainerBuilder> onConfigureServicesAction);
        IHostApplication OnPrepare(Action<IContainer> onPrepareAction);

        IHostApplication AddServer<TInterface, TServer>()
            where TServer : class, IStartStopObject
            where TInterface : notnull;
        object[] PopulateArguments(MethodInfo methodInfo, object[] args);
        
        TInterface Get<TInterface>() where TInterface : notnull;
        object Get(Type interfaceType);

        Task RunAsync();
    }
}
