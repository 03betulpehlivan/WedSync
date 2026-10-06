using Microsoft.AspNetCore.Mvc;
using dugunsalonu.Data;
using dugunsalonu.Hubs;
using dugunsalonu.Models;
using dugunsalonu.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace dugunsalonu.Controllers
{
    public class SlideshowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SlideshowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /slideshow/index/SLUG
        [HttpGet("slideshow/index/{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            var weddingEvent = await _context.WeddingEvents.FirstOrDefaultAsync(e => e.Slug == slug);
            if (weddingEvent == null) return NotFound("Etkinlik bulunamadı.");

            int storageDays = PlanConfig.GetStorageDaysInt(weddingEvent.PlanType);
            var storageEndDate = weddingEvent.EventDate.Date.AddDays(storageDays);
            bool isStorageExpired = System.DateTime.Today > storageEndDate;

            var query = _context.GuestEntries
                .Where(e => e.EventId == weddingEvent.Id && e.IsApproved)
                .OrderByDescending(e => e.UploadedAt)
                .Select(e => new { e.PhotoPath, e.Message, e.GuestName });
            var approvedPhotos = isStorageExpired ? await query.Take(0).ToListAsync() : await query.ToListAsync();

            ViewBag.Event = weddingEvent;
            ViewBag.IsStorageExpired = isStorageExpired;
            return View(approvedPhotos);
        }
    }
}
