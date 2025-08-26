using System.Text.Json;
using DmxControlApp.Models;

namespace DmxControlApp.Services;

public interface IStateService
{
    AppState Get();
    Task<AppState> SaveAsync(AppState state, CancellationToken cancellationToken = default);
}

public sealed class StateService : IStateService
{
    private readonly string _filePath;
    private readonly object _lock = new object();
    private AppState _state = new AppState();

    public StateService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "data", "state.json");
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var s = JsonSerializer.Deserialize<AppState>(json);
                if (s != null) _state = s;
            }
        }
        catch { }
    }

    public AppState Get()
    {
        lock (_lock) return _state;
    }

    public async Task<AppState> SaveAsync(AppState state, CancellationToken cancellationToken = default)
    {
        lock (_lock) _state = state;
        try
        {
            var dir = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
        catch { }
        return _state;
    }
}

