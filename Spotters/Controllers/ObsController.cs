using Microsoft.AspNetCore.Mvc;
using Spotters.Models;

namespace Spotters.Controllers;

public sealed class ObsController : Controller
{
    private readonly AppConfig _config;

    public ObsController(AppConfig config)
    {
        _config = config;
    }

    [Route("/obs/{username}/{character}")]
    public IActionResult Index(string username, string character)
    {
        if (!_config.Users.Select(it => it.UserName.ToUpper()).Contains(username.ToUpper()) ||
            !_config.Users.Where(it => it.UserName.Equals(username, StringComparison.CurrentCultureIgnoreCase))
            .SelectMany(it => it.Characters).Any(it => (it.Name?.ToUpper()).Equals(character, StringComparison.CurrentCultureIgnoreCase)))
        {
            return NotFound();
        }

        return View(new ObsVM { Username = username, Character = character });
    }
}
