using Microsoft.AspNetCore.SignalR.Client;

namespace Spotters.Services;

public sealed class SignalRService
{
    public HubConnection StartSignalRConnection(AppConfig _config)
    {
        var connection = new HubConnectionBuilder()
                    .WithUrl($"http://localhost:{_config.Port}/hub/audio")
                    .WithAutomaticReconnect()
                    .Build();

        connection.StartAsync().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                MessageBox.Show("Error connecting to Spotters webserver", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        return connection;
    }
}
