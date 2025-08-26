using System.Security.Cryptography;

namespace DmxControlApp.Services;

public interface IAiPresetService
{
    Task<byte[]> GeneratePresetAsync(string? prompt, int channelCount, CancellationToken cancellationToken = default);
}

public sealed class AiPresetService : IAiPresetService
{
    public Task<byte[]> GeneratePresetAsync(string? prompt, int channelCount, CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(channelCount, 1, 512);
        var values = new byte[count];

        // Deterministic-ish seed from prompt
        int seed = 12345;
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            foreach (var ch in prompt!) seed = unchecked(seed * 31 + ch);
        }
        var rng = new Random(seed);

        // Simple generative logic: choose a palette shape based on keywords
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

        return Task.FromResult(values);
    }
}

