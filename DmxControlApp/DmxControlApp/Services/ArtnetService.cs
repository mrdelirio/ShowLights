using System.Net;
using System.Net.Sockets;

namespace DmxControlApp.Services;

public interface IArtnetService
{
    Task SendDmxAsync(int universe, byte[] values, string? targetIp = null, CancellationToken cancellationToken = default);
}

public sealed class ArtnetService : IArtnetService, IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly string _defaultTargetIp;
    private const int ArtnetPort = 6454;

    public ArtnetService(IConfiguration configuration)
    {
        _udpClient = new UdpClient
        {
            EnableBroadcast = true
        };
        _defaultTargetIp = configuration["ArtNet:TargetIp"] ?? "255.255.255.255";
    }

    public async Task SendDmxAsync(int universe, byte[] values, string? targetIp = null, CancellationToken cancellationToken = default)
    {
        if (universe < 0 || universe > 32767)
        {
            throw new ArgumentOutOfRangeException(nameof(universe), "Universe must be between 0 and 32767");
        }

        var length = Math.Min(values.Length, 512);
        var packetLength = 18 + length; // ArtDmx header size is 18 bytes
        var buffer = new byte[packetLength];

        // ID: "Art-Net\0"
        buffer[0] = (byte)'A';
        buffer[1] = (byte)'r';
        buffer[2] = (byte)'-';
        buffer[3] = (byte)'N';
        buffer[4] = (byte)'e';
        buffer[5] = (byte)'t';
        buffer[6] = 0x00;

        // OpCode: OpOutput / ArtDmx = 0x5000 (little endian)
        buffer[8] = 0x00;
        buffer[9] = 0x50;

        // ProtVer Hi/Lo (14)
        buffer[10] = 0x00; // Hi
        buffer[11] = 0x0E; // Lo (14)

        // Sequence
        buffer[12] = 0x00;

        // Physical
        buffer[13] = 0x00;

        // Universe (lo, hi)
        buffer[14] = (byte)(universe & 0xFF);
        buffer[15] = (byte)((universe >> 8) & 0xFF);

        // Length (hi, lo)
        buffer[16] = (byte)((length >> 8) & 0xFF);
        buffer[17] = (byte)(length & 0xFF);

        // Data
        Buffer.BlockCopy(values, 0, buffer, 18, length);

        var ip = IPAddress.Parse(string.IsNullOrWhiteSpace(targetIp) ? _defaultTargetIp : targetIp);
        await _udpClient.SendAsync(buffer, buffer.Length, new IPEndPoint(ip, ArtnetPort));
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }
}

