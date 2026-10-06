using Microsoft.AspNetCore.SignalR;

namespace dugunsalonu.Hubs
{
    /// <summary>
    /// Admin paneli için gerçek zamanlı bildirimler (yeni fotoğraf onay bekliyor vb.)
    /// </summary>
    public class AdminHub : Hub
    {
        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
        }
    }
}
