using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spotters.Hubs;
using Spotters.Services;

namespace Spotters;

public sealed class WebServer
{
    private IHost? _host;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _host is not null;

    public async Task StartAsync(AppConfig config)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSignalR();
        builder.WebHost.UseUrls($"http://localhost:{config.Port}");

        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<ConfigService>();
        builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
            app.UseStatusCodePages();
        }

        app.UseStaticFiles();
        app.UseRouting();

        app.MapHub<AudioHub>("/hub/audio");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        _host = app;
        _ = app.RunAsync(_cts.Token);

        await Task.Delay(100);
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        try
        {
            _cts?.Cancel();
            await _host!.StopAsync();
            _host.Dispose();
        }
        finally
        {
            _host = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IProblemDetailsService problemDetailsService,
            IHostEnvironment env)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);

            int status = StatusCodes.Status500InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Title = "Unexpected error",
                Detail = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                Type = exception.GetType().Name,
                Instance = httpContext.Request.Path
            };

            await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });

            return true;
        }
    }
}