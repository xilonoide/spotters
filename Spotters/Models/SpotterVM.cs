namespace Spotters.Models;

public class SpotterVM
{
    public string Username { get; set; } = string.Empty;
    public List<Spotter> Spotters { get; set; } = new List<Spotter>();
}
