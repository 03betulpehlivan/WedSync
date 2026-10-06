using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using dugunsalonu.Data;
using dugunsalonu.Models;
using dugunsalonu.Services;
using dugunsalonu.ViewModels;
using dugunsalonu.Hubs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace dugunsalonu.Controllers
{
    [AllowAnonymous]
    public class GuestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IHubContext<AdminHub> _adminHub;

        public GuestController(ApplicationDbContext context, IFileService fileService, IHubContext<AdminHub> adminHub)
        {
            _context = context;
            _fileService = fileService;
            _adminHub = adminHub;
        }

        // DEBUG: Test Authentication Bypass
        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Content("Merhaba Dünya! Giriş yapmadan buradasın.");
        }

        // GET: Guest/Index/SLUG (Digital Album View)
        [HttpGet("event/{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            var weddingEvent = await _context.WeddingEvents
                .FirstOrDefaultAsync(e => e.Slug == slug);

            if (weddingEvent == null) return NotFound("Etkinlik bulunamadı.");

            // Depolama süresi: Plan bazlı (Free: 7, Pro/SalonBusiness: 365 gün). Süre dolduysa albüm boş gösterilir.
            int storageDays = PlanConfig.GetStorageDaysInt(weddingEvent.PlanType);
            var storageEndDate = weddingEvent.EventDate.Date.AddDays(storageDays);
            bool isStorageExpired = DateTime.Today > storageEndDate;

            var approvedEntries = isStorageExpired
                ? new List<GuestEntry>()
                : await _context.GuestEntries
                    .Where(e => e.EventId == weddingEvent.Id && e.IsApproved)
                    .OrderByDescending(e => e.UploadedAt)
                    .ToListAsync();

            ViewBag.Event = weddingEvent;
            ViewBag.IsStorageExpired = isStorageExpired;
            ViewBag.StorageEndDate = storageEndDate;
            return View(approvedEntries);
        }

        // GET: Guest/Upload/SLUG (Upload View)
        [HttpGet("event/{slug}/upload")]
        public async Task<IActionResult> Upload(string slug)
        {
            var weddingEvent = await _context.WeddingEvents.FirstOrDefaultAsync(e => e.Slug == slug);
            if (weddingEvent == null) return NotFound();

            int uploadDays = PlanConfig.GetUploadDaysInt(weddingEvent.PlanType);
            var uploadDeadline = weddingEvent.EventDate.Date.AddDays(uploadDays);
            bool uploadPeriodExpired = DateTime.Today > uploadDeadline;

            ViewBag.Event = weddingEvent;
            ViewBag.UploadPeriodExpired = uploadPeriodExpired;
            ViewBag.UploadDeadline = uploadDeadline;
            return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
        }

        [HttpPost("event/{slug}/upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string slug, GuestEntryViewModel model)
        {
            var weddingEvent = await _context.WeddingEvents.FirstOrDefaultAsync(e => e.Slug == slug);
            if (weddingEvent == null) return NotFound();

            // Yükleme süresi kontrolü: Etkinlik tarihinden itibaren X gün (Free: 1, Pro/SalonBusiness: 30)
            int uploadDays = PlanConfig.GetUploadDaysInt(weddingEvent.PlanType);
            var uploadDeadline = weddingEvent.EventDate.Date.AddDays(uploadDays);
            if (DateTime.Today > uploadDeadline)
            {
                ModelState.AddModelError(string.Empty, $"Yükleme süresi dolmuştur. Bu etkinlik için yükleme {uploadDeadline:dd.MM.yyyy} tarihine kadar yapılabiliyordu.");
                ViewBag.Event = weddingEvent;
                ViewBag.UploadPeriodExpired = true;
                ViewBag.UploadDeadline = uploadDeadline;
                return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
            }

            // Çoklu veya tek dosya: Photos listesini oluştur
            var filesToUpload = new List<IFormFile>();
            if (model.Photos != null && model.Photos.Count > 0)
                filesToUpload.AddRange(model.Photos.Where(f => f != null && f.Length > 0));
            else if (model.Photo != null && model.Photo.Length > 0)
                filesToUpload.Add(model.Photo);

            // Sadece mesaj gönderimi (fotoğrafsız)
            if (filesToUpload.Count == 0 && !string.IsNullOrWhiteSpace(model.Message))
            {
                int msgCount = await _context.GuestEntries.CountAsync(e => e.EventId == weddingEvent.Id);
                int msgLimit = PlanConfig.GetUploadLimitInt(weddingEvent.PlanType);
                if (msgCount >= msgLimit)
                {
                    ModelState.AddModelError(string.Empty, "Bu etkinlik için yükleme limitine ulaşıldı.");
                    ViewBag.Event = weddingEvent;
                    return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
                }
                _context.GuestEntries.Add(new GuestEntry
                {
                    EventId = weddingEvent.Id,
                    GuestName = model.GuestName ?? "Misafir",
                    Message = model.Message,
                    PhotoPath = null,
                    UploadedAt = DateTime.Now,
                    IsApproved = false, // Mesajlar da moderasyon onayı beklemeli
                    GuestSessionId = model.GuestSessionId
                });
                await _context.SaveChangesAsync();

                // Admin paneline bildirim gönder
                var pendingCount = await _context.GuestEntries.CountAsync(e => e.EventId == weddingEvent.Id && !e.IsApproved);
                await _adminHub.Clients.Group($"event-{weddingEvent.Id}").SendAsync("NewPendingItems", pendingCount);

                TempData["SuccessMessage"] = "Mesajınız gönderildi. Onaylandıktan sonra ekrana yansıyacaktır.";
                return RedirectToAction("Success", new { slug = slug });
            }

            if (filesToUpload.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Lütfen en az bir fotoğraf seçin veya mesaj yazın.");
                ViewBag.Event = weddingEvent;
                return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
            }

            // Plan limiti kontrolü (Free: 50, Pro/SalonBusiness: sınırsız)
            int currentCount = await _context.GuestEntries.CountAsync(e => e.EventId == weddingEvent.Id);
            int limit = PlanConfig.GetUploadLimitInt(weddingEvent.PlanType);

            int remainingSlots = limit - currentCount;
            if (remainingSlots <= 0)
            {
                ModelState.AddModelError(string.Empty, "Bu etkinlik için yükleme limitine ulaşıldı.");
                ViewBag.LimitReached = true;
                ViewBag.Event = weddingEvent;
                return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
            }

            // En fazla kalan slota kadar yükle (ör. 10 ile sınırla tek seferde)
            int maxPerRequest = Math.Min(10, remainingSlots);
            var files = filesToUpload.Take(maxPerRequest).ToList();

            int successCount = 0;
            string? lastError = null;

            foreach (var file in files)
            {
                try
                {
                    string filePath = await _fileService.SaveFileAsync(file, "guests");
                    var guestEntry = new GuestEntry
                    {
                        EventId = weddingEvent.Id,
                        GuestName = model.GuestName ?? "Misafir",
                        Message = model.Message,
                        PhotoPath = filePath,
                        UploadedAt = DateTime.Now,
                        IsApproved = false,  // Moderasyon onayı bekleyecek
                        GuestSessionId = model.GuestSessionId
                    };
                    _context.GuestEntries.Add(guestEntry);
                    successCount++;
                }
                catch (InvalidOperationException ex)
                {
                    lastError = ex.Message;
                    break;
                }
                catch (ArgumentException ex)
                {
                    lastError = ex.Message;
                    break;
                }
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
            {
                // Admin paneline gerçek zamanlı bildirim gönder
                var pendingCount = await _context.GuestEntries.CountAsync(e => e.EventId == weddingEvent.Id && !e.IsApproved);
                await _adminHub.Clients.Group($"event-{weddingEvent.Id}").SendAsync("NewPendingItems", pendingCount);

                TempData["SuccessMessage"] = successCount == 1
                    ? "Fotoğrafınız yüklendi. Onaylandıktan sonra albümde görünecektir."
                    : $"{successCount} fotoğraf yüklendi. Onaylandıktan sonra albümde görünecektir.";
                return RedirectToAction("Success", new { slug = slug });
            }

            ModelState.AddModelError(string.Empty, lastError ?? "Yükleme sırasında bir hata oluştu.");
            ViewBag.Event = weddingEvent;
            return View(new GuestEntryViewModel { EventId = weddingEvent.Id });
        }
        // GET: Guest/Success/SLUG
        [HttpGet("event/{slug}/success")]
        public async Task<IActionResult> Success(string slug)
        {
            var weddingEvent = await _context.WeddingEvents.FirstOrDefaultAsync(e => e.Slug == slug);
            if (weddingEvent == null) return NotFound();

            ViewBag.Event = weddingEvent;
            return View();
        }
    }
}
