using Microsoft.AspNetCore.SignalR;

namespace Spotters.Hubs;

public class AudioHub : Hub
{
    public async Task SendVolume(string user, float volume, string character, bool visible)
    {
        await Clients.Others.SendAsync("ReceiveVolume", user, volume * 10, character, visible);
    }
}
