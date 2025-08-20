using System.Text.Json.Serialization;

namespace Spotters;

public sealed class AppConfig
{
    public int Port { get; set; } = 1234;
    public List<UserAudioMapping> Users { get; set; } = new();
}

public sealed class UserAudioMapping
{
    public string UserName { get; set; } = string.Empty;
    public string? AudioDeviceProductName { get; set; }
    public List<Character> Characters { get; set; } = new();

    [JsonIgnore]
    public Character? ActiveCharacter => Characters.SingleOrDefault(c => c.Active);
}

public sealed class Character
{
    public string? Name { get; set; }
    public bool Visible { get; set; }
    public bool Active { get; set; }
}