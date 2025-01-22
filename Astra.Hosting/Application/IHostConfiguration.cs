using System.Runtime.InteropServices;

namespace Astra.Hosting.Application;

public interface IHostConfiguration
{
    Task<T> GetValueAsync<T>(string key, [Optional] T? defaultValue, [Optional] CancellationToken cancellationToken);
    T GetValue<T>(string key, [Optional] T? defaultValue);
}