using System.Text.Json;
using DmxControlApp.Client.Services;

namespace DmxControlApp.Services;

public sealed class LocalStorageStub : ILocalStorage
{
    private static readonly Dictionary<string, string> _storage = new();

    public Task<T?> GetItemAsync<T>(string key)
    {
        if (_storage.TryGetValue(key, out var json))
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(json);
                return Task.FromResult(result);
            }
            catch
            {
                return Task.FromResult<T?>(default);
            }
        }
        return Task.FromResult<T?>(default);
    }

    public Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        _storage[key] = json;
        return Task.CompletedTask;
    }

    public Task RemoveItemAsync(string key)
    {
        _storage.Remove(key);
        return Task.CompletedTask;
    }

    public T? GetItem<T>(string key)
    {
        if (_storage.TryGetValue(key, out var json))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    public void SetItem<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        _storage[key] = json;
    }

    public void RemoveItem(string key)
    {
        _storage.Remove(key);
    }
}
