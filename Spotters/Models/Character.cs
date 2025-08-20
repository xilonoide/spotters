namespace Spotters.Models;

public record Character
{
    public string? Name { get; set; }
    public bool Visible { get; set; }
    public bool Active { get; set; }
}

