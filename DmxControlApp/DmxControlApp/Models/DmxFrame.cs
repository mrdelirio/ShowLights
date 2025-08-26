namespace DmxControlApp.Models;

public record DmxFrame(int Universe, byte[] Values, string? TargetIp);

