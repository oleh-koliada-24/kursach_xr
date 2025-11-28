using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs
{
    public class AnonymizationHub : Hub
    {
        public async Task SendProgress(string sessionId, int currentFrame, int totalFrames, int percentage)
        {
            await Clients.All.SendAsync("ReceiveProgress", sessionId, currentFrame, totalFrames, percentage);
        }
    }
}
