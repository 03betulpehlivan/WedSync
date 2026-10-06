using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace dugunsalonu.Hubs
{
    public class GuestHub : Hub
    {
        public async Task JoinGuestGroup(string guestSessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"guest-{guestSessionId}");
        }
    }
}
