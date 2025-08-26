namespace DmxControlApp.Models;

public sealed class SceneModel
{
    public string Name { get; set; } = string.Empty;
    public int Universe { get; set; }
    public byte[] Values { get; set; } = new byte[512];
}

public sealed class PatchModel
{
    public int Universe { get; set; }
    public int ActiveChannels { get; set; } = 32;
    public string[] Labels { get; set; } = Enumerable.Repeat(string.Empty, 512).ToArray();
}

public sealed class AppState
{
    public List<SceneModel> Scenes { get; set; } = new();
    public List<PatchModel> Patches { get; set; } = new();
    public List<CueModel> Cues { get; set; } = new();
}

public sealed class CueModel
{
    public string Name { get; set; } = string.Empty;
    public int Universe { get; set; }
    public byte[] Values { get; set; } = Array.Empty<byte>();
    public int FadeMs { get; set; }
    public int HoldMs { get; set; }
}

