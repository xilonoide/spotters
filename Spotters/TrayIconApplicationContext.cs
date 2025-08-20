using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualBasic.Logging;
using NAudio.Wave;
using Spotters.Services;
using System.Diagnostics;

namespace Spotters;

public sealed class TrayIconApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly WebServer _webServer;
    private readonly ConfigService _configService;
    private readonly SignalRService _signalRService;
    private readonly AudioService _audioService;

    private AppConfig _config = new();

    public TrayIconApplicationContext()
    {
        _webServer = new WebServer();
        _configService = new ConfigService();
        _signalRService = new SignalRService();
        _audioService = new AudioService();

        // Use a bundled icon; replace with your own resource if needed.
        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Spotters",
            Icon = LoadIconSafely()
        };

        _trayIcon.ContextMenuStrip = BuildMenu();
    }

    public async Task InitializeAsync()
    {
        try
        {
            _config = await _configService.LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load configuration. Default values will be used.\n{ex}", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _config = new AppConfig();
        }

        // Auto-start the web server on launch
        await EnsureServerStartedAsync();
        RefreshMenu();

        // connect tray to webserver SignalR hub
        var signalRconnection = _signalRService.StartSignalRConnection(_config);

        // Initialize audio capture service
        _audioService.InitializeAudioCapture(signalRconnection, _config.Users);
    }

    private Icon LoadIconSafely()
    {
        try
        {
            using var stream = typeof(TrayIconApplicationContext).Assembly.GetManifestResourceStream("Spotters.Resources.app.ico");
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch { /* ignore and fallback */ }

        return SystemIcons.Application; // fallback
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        if (!_webServer.IsRunning)
        {
            var startItem = new ToolStripMenuItem("Start web server", null, async (_, _) =>
            {
                await EnsureServerStartedAsync();
                RefreshMenu();
            });
            menu.Items.Add(startItem);
        }
        else
        {
            var stopItem = new ToolStripMenuItem("Stop web server", null, async (_, _) =>
            {
                await EnsureServerStoppedAsync();
                RefreshMenu();
            });
            menu.Items.Add(stopItem);
        }

        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Settings", null, async (_, _) =>
        {
            await ShowSettingsAsync();
        });
        menu.Items.Add(settingsItem);

        var exitItem = new ToolStripMenuItem("Exit", null, async (_, _) =>
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Spotters", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                await EnsureServerStoppedAsync();
                _trayIcon.Visible = false;
                Application.Exit();
            }
        });
        menu.Items.Add(exitItem);

        return menu;
    }

    private void RefreshMenu()
    {
        _trayIcon.ContextMenuStrip = BuildMenu();
    }

    private async Task EnsureServerStartedAsync()
    {
        if (_webServer.IsRunning) return;

        try
        {
            await _webServer.StartAsync(_config); // pass config if needed for DI
            _trayIcon.BalloonTipTitle = "Spotters";
            _trayIcon.BalloonTipText = $"Web server started on http://localhost:{_config.Port}";
            _trayIcon.ShowBalloonTip(3000);

            Process.Start(new ProcessStartInfo
            {
                FileName = $"http://localhost:{_config.Port}",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start web server:\n{ex}", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EnsureServerStoppedAsync()
    {
        if (!_webServer.IsRunning) return;

        try
        {
            await _webServer.StopAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to stop web server:\n{ex}", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowSettingsAsync()
    {
        using var form = new UI.ConfigForm(_config);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _config = form.Result!;
            try
            {
                await _configService.SaveAsync(_config);
                MessageBox.Show("Configuration saved successfully. Restart needed", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await EnsureServerStoppedAsync();
                _trayIcon.Visible = false;
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration:\n{ex}", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}