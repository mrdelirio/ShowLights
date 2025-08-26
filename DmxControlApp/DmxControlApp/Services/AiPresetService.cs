using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DmxControlApp.Services;

public interface IAiPresetService
{
    Task<byte[]> GeneratePresetAsync(string? prompt, int channelCount, CancellationToken cancellationToken = default);
}

public sealed class AiPresetService : IAiPresetService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAppSettingsService _settingsService;

    public AiPresetService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IAppSettingsService settingsService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _settingsService = settingsService;
    }

    public async Task<byte[]> GeneratePresetAsync(string? prompt, int channelCount, CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(channelCount, 1, 512);

        // Try provider if configured
        var provider = _settingsService.Get().AI.Provider?.Trim();
        if (!string.IsNullOrWhiteSpace(provider))
        {
            try
            {
                if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    var values = await GenerateWithOpenAiAsync(prompt, count, cancellationToken);
                    if (values != null) return values;
                }
                else if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
                {
                    var values = await GenerateWithOllamaAsync(prompt, count, cancellationToken);
                    if (values != null) return values;
                }
            }
            catch
            {
                // Ignore and fallback below
            }
        }

        return GenerateFallback(prompt, count);
    }

    private async Task<byte[]?> GenerateWithOpenAiAsync(string? prompt, int count, CancellationToken ct)
    {
        var settingsOpenAi = _settingsService.Get().AI.OpenAI;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? settingsOpenAi.ApiKey ?? _configuration["AI:OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var baseUrl = settingsOpenAi.BaseUrl ?? _configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        var model = settingsOpenAi.Model ?? _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var systemPrompt = $"You are a DMX lighting assistant. Output ONLY a JSON array of {count} integers between 0 and 255. No prose.";
        var userPrompt = string.IsNullOrWhiteSpace(prompt) ? "Create a balanced DMX preset." : prompt!;

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        var body = new
        {
            model,
            temperature = 0.6,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (content is null) return null;
        return TryParseValues(content, count);
    }

    private async Task<byte[]?> GenerateWithOllamaAsync(string? prompt, int count, CancellationToken ct)
    {
        var settingsOllama = _settingsService.Get().AI.Ollama;
        var baseUrl = settingsOllama.BaseUrl ?? _configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = settingsOllama.Model ?? _configuration["AI:Ollama:Model"] ?? "llama3.1";

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));

        var systemPrompt = $"Output ONLY a JSON array of {count} integers 0-255. No prose.";
        var fullPrompt = $"{systemPrompt}\nPrompt: {prompt}";

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/generate");
        var body = new
        {
            model,
            prompt = fullPrompt,
            stream = false
        };
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var content = doc.RootElement.GetProperty("response").GetString();
        if (content is null) return null;
        return TryParseValues(content, count);
    }

    private static byte[] GenerateFallback(string? prompt, int count)
    {
        var values = new byte[count];
        int seed = 12345;
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            foreach (var ch in prompt!) seed = unchecked(seed * 31 + ch);
        }
        var rng = new Random(seed);
        var lower = (prompt ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("strobo") || lower.Contains("strobe"))
        {
            for (int i = 0; i < count; i++) values[i] = (byte)(i % 2 == 0 ? 255 : 0);
        }
        else if (lower.Contains("warm") || lower.Contains("caldo"))
        {
            for (int i = 0; i < count; i++) values[i] = (byte)(180 + rng.Next(0, 75));
        }
        else if (lower.Contains("cool") || lower.Contains("freddo"))
        {
            for (int i = 0; i < count; i++) values[i] = (byte)(rng.Next(0, 120));
        }
        else if (lower.Contains("wave") || lower.Contains("onda"))
        {
            for (int i = 0; i < count; i++) values[i] = (byte)(127 + 126 * Math.Sin((i / (double)count) * Math.PI * 2));
        }
        else
        {
            for (int i = 0; i < count; i++) values[i] = (byte)rng.Next(0, 256);
        }
        return values;
    }

    private static byte[]? TryParseValues(string content, int count)
    {
        // Extract JSON array from content
        var match = Regex.Match(content, "\\[(.*?)\\]", RegexOptions.Singleline);
        if (!match.Success) return null;
        try
        {
            var arr = JsonSerializer.Deserialize<int[]>(match.Value);
            if (arr == null || arr.Length == 0) return null;
            var trimmed = arr.Take(count).Select(v => (byte)Math.Clamp(v, 0, 255)).ToArray();
            if (trimmed.Length < count)
            {
                var list = new List<byte>(trimmed);
                while (list.Count < count) list.Add(0);
                return list.ToArray();
            }
            return trimmed;
        }
        catch
        {
            return null;
        }
    }
}

