using System.Text.Json;

namespace DmxControlApp.Services;

public interface IAppSettingsService
{
    AppSettings Get();
    AppSettings GetSanitized();
    Task<AppSettings> UpdateAsync(AppSettings newSettings, CancellationToken cancellationToken = default);
}

public sealed class AppSettingsService : IAppSettingsService
{
    private readonly string _settingsFilePath;
    private readonly object _lock = new object();
    private AppSettings _current;

    public AppSettingsService(IConfiguration configuration, IWebHostEnvironment env)
    {
        // Initialize from appsettings.json
        var artNet = new ArtNetSettings
        {
            TargetIp = configuration["ArtNet:TargetIp"] ?? "255.255.255.255"
        };
        var ai = new AISettings
        {
            Provider = configuration["AI:Provider"] ?? string.Empty,
            OpenAI = new OpenAISettings
            {
                BaseUrl = configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1",
                Model = configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini",
                ApiKey = configuration["AI:OpenAI:ApiKey"] ?? string.Empty
            },
            Ollama = new OllamaSettings
            {
                BaseUrl = configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434",
                Model = configuration["AI:Ollama:Model"] ?? "llama3.1"
            }
        };
        _current = new AppSettings { ArtNet = artNet, AI = ai };

        // File path under app data
        _settingsFilePath = Path.Combine(env.ContentRootPath, "data", "settings.json");
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var fromFile = JsonSerializer.Deserialize<AppSettings>(json);
                if (fromFile != null)
                {
                    // Do not overwrite ApiKey from file if empty; prefer env/appsettings
                    if (string.IsNullOrWhiteSpace(fromFile.AI.OpenAI.ApiKey))
                        fromFile.AI.OpenAI.ApiKey = _current.AI.OpenAI.ApiKey;
                    _current = fromFile;
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    public AppSettings Get()
    {
        lock (_lock)
        {
            return _current.Clone();
        }
    }

    public AppSettings GetSanitized()
    {
        var copy = Get();
        copy.AI.OpenAI.ApiKey = string.Empty;
        return copy;
    }

    public async Task<AppSettings> UpdateAsync(AppSettings newSettings, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Keep existing API key if missing in update
            if (string.IsNullOrWhiteSpace(newSettings.AI.OpenAI.ApiKey))
            {
                newSettings.AI.OpenAI.ApiKey = _current.AI.OpenAI.ApiKey;
            }
            _current = newSettings.Clone();
        }

        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath)!;
            Directory.CreateDirectory(dir);
            var toPersist = newSettings.Clone();
            // Do not persist API key to disk for safety in this demo
            toPersist.AI.OpenAI.ApiKey = string.Empty;
            var json = JsonSerializer.Serialize(toPersist, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
        }
        catch
        {
            // ignore write errors
        }

        return GetSanitized();
    }
}

public sealed class AppSettings
{
    public ArtNetSettings ArtNet { get; set; } = new ArtNetSettings();
    public AISettings AI { get; set; } = new AISettings();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            ArtNet = new ArtNetSettings { TargetIp = ArtNet.TargetIp },
            AI = new AISettings
            {
                Provider = AI.Provider,
                OpenAI = new OpenAISettings
                {
                    BaseUrl = AI.OpenAI.BaseUrl,
                    Model = AI.OpenAI.Model,
                    ApiKey = AI.OpenAI.ApiKey
                },
                Ollama = new OllamaSettings
                {
                    BaseUrl = AI.Ollama.BaseUrl,
                    Model = AI.Ollama.Model
                }
            }
        };
    }
}

public sealed class ArtNetSettings
{
    public string TargetIp { get; set; } = "255.255.255.255";
}

public sealed class AISettings
{
    public string Provider { get; set; } = string.Empty; // None | OpenAI | Ollama
    public OpenAISettings OpenAI { get; set; } = new OpenAISettings();
    public OllamaSettings Ollama { get; set; } = new OllamaSettings();
}

public sealed class OpenAISettings
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1";
}

