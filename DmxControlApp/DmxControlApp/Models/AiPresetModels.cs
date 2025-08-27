namespace DmxControlApp.Models;

public record AiPresetRequest(string? Prompt, int Channels = 16);
public record AiPresetResponse(byte[] Values);

