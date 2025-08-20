using Microsoft.AspNetCore.Mvc;
using Spotters.Models;

namespace Spotters.Controllers;

public sealed class HomeController : Controller
{
    private readonly AppConfig _config;

    public HomeController(AppConfig config)
    {
        _config = config;
    }

    public IActionResult Index()
    {
        var model = new HomeVM
        {
            Spotters = _config.Users.Select(it => new Spotter
            {
                Name = it.UserName,
                Characters = it.Characters.Select(c => new Models.Character
                {
                    Name = c.Name,
                    Visible = c.Visible,
                    Active = c.Active
                }).ToList()
            }).ToList()
        };

        return View(model);
    }
}
