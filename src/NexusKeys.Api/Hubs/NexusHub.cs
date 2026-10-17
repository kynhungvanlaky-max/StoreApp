using Microsoft.AspNetCore.SignalR;

namespace GameStore.API.Hubs
{
    public class NexusHub : Hub
    {
        // Hub class can contain methods to handle messages from clients.
        // For broadcasting from the server to clients (e.g. from controllers/endpoints), 
        // we can simply inject IHubContext<NexusHub> and call it directly.
        // This class is required to establish the connection endpoint.
        
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
