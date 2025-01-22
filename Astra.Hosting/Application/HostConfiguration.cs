using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Astra.Hosting.Application;

public sealed class HostConfiguration : IHostConfiguration
{
    private readonly string _fileName;
    private HostConfiguration(string fileName)
    {
        _fileName = fileName;
        if (!File.Exists(_fileName))
            throw new FileNotFoundException($"The file '{_fileName}' was not found.");
    }
    
    public static HostConfiguration FromFile(string fileName) => new HostConfiguration(fileName);
    
    public T GetValue<T>(string key, [Optional] T? defaultValue) => GetValueAsync<T>(key, defaultValue).Result;
    public async Task<T> GetValueAsync<T>(string key, [Optional] T? defaultValue, [Optional] CancellationToken cancellationToken)
    {
        var dictionary = await ToDictionaryAsync(cancellationToken);
        if (dictionary.Count == 0 || !dictionary.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"The key '{key}' was not found, or the configuration file has no values defined.");

        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            return (T?)converter.ConvertFromString(value.ToString() ?? throw new InvalidOperationException()) 
                ?? throw new InvalidOperationException();
        }
        using var serializedMemoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(serializedMemoryStream, value, cancellationToken: cancellationToken);
            
        return (await JsonSerializer.DeserializeAsync<T>(
            serializedMemoryStream, 
            cancellationToken: cancellationToken
        ) ?? defaultValue) ?? throw new NullReferenceException(
            $"The value when returning HostConfiguration::{key} ({typeof(T).Name}) was null."
        );
    }

    private async Task<Dictionary<string, object>> ToDictionaryAsync(CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(await File.ReadAllBytesAsync(_fileName, cancellationToken));
        return await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(
            memoryStream, 
            cancellationToken: cancellationToken
        ) ?? throw new InvalidOperationException();
    }
}