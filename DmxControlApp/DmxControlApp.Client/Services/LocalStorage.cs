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

    public T? GetItem<T>(string key)
    {
        var json = GetItemRaw(key);
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

    public void SetItem<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        SetItemRaw(key, json);
    }

    public void RemoveItem(string key)
    {
        _ = _jsRuntime.InvokeVoidAsync("eval", $"localStorage.removeItem('{EscapeJs(key)}')");
    }

    private string? GetItemRaw(string key)
    {
        return _jsRuntime.InvokeAsync<string>("eval", $"localStorage.getItem('{EscapeJs(key)}')").AsTask().GetAwaiter().GetResult();
    }

    private void SetItemRaw(string key, string json)
    {
        _ = _jsRuntime.InvokeVoidAsync("eval", $"localStorage.setItem('{EscapeJs(key)}', '{EscapeJs(json)}')");
    }

    private static string EscapeJs(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

