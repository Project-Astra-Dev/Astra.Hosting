using System.Reflection;
using Autofac;

namespace Astra.Hosting.Application
{
    public interface IHostApplication
    {
        IHostApplication ConfigureServices(Action<ContainerBuilder> onConfigureServicesAction);

        IHostApplication AddServer<TInterface, TServer>()
            where TServer : class, IStartStopObject
            where TInterface : notnull;
        object[] PopulateArguments(MethodInfo methodInfo, object[] args);

        Task RunAsync();
    }
}
