using Microsoft.AspNetCore.Mvc;
using Spotters.Models;
using Spotters.Services;

namespace Spotters.Controllers;

public sealed class SpotterController : Controller
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;

    public SpotterController(AppConfig config, ConfigService configService)
    {
        _config = config;
        _configService = configService;
    }

    [Route("/spotter/{username}")]
    public IActionResult Index(string username)
    {
        if (!_config.Users.Select(it => it.UserName.ToUpper()).Contains(username.ToUpper()))
        {
            return NotFound();
        }

        var model = new Models.SpotterVM
        {
            Username = username,
            Spotters = _config.Users
                .Select(it => new Models.Spotter
                {
                    Name = it.UserName,
                    Characters = it.Characters.Select(c => new Models.Character
                    {
                        Name = c.Name,
                        Visible = c.Visible,
                        Active = c.Active
                    }).ToList()
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPatch]
    [Route("/spotter/update-characters/{username}")]
    public async Task<IActionResult> UpdateCharacters(string username, [FromBody] List<Character> Characters)
    {
        foreach (var character in Characters)
        {
            var characterInConfig = _config.Users.Single(it => it.UserName == username).Characters.Single(it => it.Name == character.Name);

            characterInConfig.Visible = character.Visible;
            characterInConfig.Active = character.Active;
        }
        
        await _configService.SaveAsync(_config);

        return Ok();
    }
}
