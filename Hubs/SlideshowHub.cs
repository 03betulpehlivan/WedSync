using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace dugunsalonu.Hubs
{
    public class SlideshowHub : Hub
    {
        // İstemciler (Slayt sayfaları) belirli bir düğün grubuna (slug) katılacak
        public async Task JoinGroup(string slug)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, slug);
        }
    }
}
