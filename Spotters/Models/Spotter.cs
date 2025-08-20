namespace Spotters.Models;

public class Spotter
{
    public string Name { get; set; } = string.Empty;
    public List<Character> Characters { get; set; } = new List<Character>();

    public string? ActiveCharacter => Characters.SingleOrDefault(c => c.Active)?.Name;
    public string ImagesPath(string CharacterName) => $"images/{Name}/{CharacterName}";
}
