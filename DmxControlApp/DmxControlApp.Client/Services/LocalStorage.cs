using System.Text.Json;
using Microsoft.JSInterop;

namespace DmxControlApp.Client.Services;

public sealed class LocalStorage : ILocalStorage
{
    private readonly IJSRuntime _jsRuntime;

    public LocalStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await GetItemRawAsync(key);
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await SetItemRawAsync(key, json);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("eval", $"localStorage.removeItem('{EscapeJs(key)}')");
    }

    // Metodi sincroni per compatibilità (non supportati in WebAssembly)
    public T? GetItem<T>(string key)
    {
        throw new PlatformNotSupportedException("Synchronous methods are not supported in WebAssembly. Use async methods instead.");
    }

    public void SetItem<T>(string key, T value)
    {
        throw new PlatformNotSupportedException("Synchronous methods are not supported in WebAssembly. Use async methods instead.");
    }

    public void RemoveItem(string key)
    {
        throw new PlatformNotSupportedException("Synchronous methods are not supported in WebAssembly. Use async methods instead.");
    }

    private async Task<string?> GetItemRawAsync(string key)
    {
        return await _jsRuntime.InvokeAsync<string>("eval", $"localStorage.getItem('{EscapeJs(key)}')");
    }

    private async Task SetItemRawAsync(string key, string json)
    {
        await _jsRuntime.InvokeVoidAsync("eval", $"localStorage.setItem('{EscapeJs(key)}', '{EscapeJs(json)}')");
    }

    private static string EscapeJs(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

