using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using dugunsalonu.Data;
using dugunsalonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using Microsoft.AspNetCore.Hosting;

namespace dugunsalonu.Controllers
{
    [Authorize] // Tüm admin sayfaları login gerektirir
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<dugunsalonu.Hubs.SlideshowHub> _hubContext;
        private readonly IHubContext<dugunsalonu.Hubs.AdminHub> _adminHub;
        private readonly IHubContext<dugunsalonu.Hubs.GuestHub> _guestHub;
        private readonly dugunsalonu.Services.IFileService _fileService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IHubContext<dugunsalonu.Hubs.SlideshowHub> hubContext, IHubContext<dugunsalonu.Hubs.AdminHub> adminHub, IHubContext<dugunsalonu.Hubs.GuestHub> guestHub, dugunsalonu.Services.IFileService fileService, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _hubContext = hubContext;
            _adminHub = adminHub;
            _guestHub = guestHub;
            _fileService = fileService;
            _config = config;
            _env = env;
        }

        /// <summary>
        /// Telefondan QR okutulduğunda erişilebilecek base URL.
        /// localhost ise yerel ağ IP'si kullanılır (aynı WiFi'deki telefonlar için).
        /// </summary>
        private string GetEventBaseUrl()
        {
            var baseUrl = _config["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                return baseUrl.TrimEnd('/');

            var host = Request.Host.Host ?? "";
            var isLocalhost = host == "localhost" || host == "127.0.0.1" || host == "::1";

            if (isLocalhost)
            {
                string? localIp = null;
                try
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                    socket.Connect("8.8.8.8", 65530);
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIp = endPoint?.Address?.ToString();
                }
                catch { }
                if (string.IsNullOrEmpty(localIp))
                {
                    try
                    {
                        var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                        localIp = hostEntry.AddressList
                            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                            ?.ToString();
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(localIp))
                {
                    var port = Request.Host.Port ?? (Request.IsHttps ? 443 : 80);
                    var scheme = Request.Scheme;
                    return $"{scheme}://{localIp}:{port}";
                }
            }

            return $"{Request.Scheme}://{Request.Host}";
        }

        // Yardımcı: Geçerli kullanıcıyı al
        private async Task<string?> GetCurrentUserId()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            return user?.Id;
        }

        // Yardımcı: Etkinliğin bu kullanıcıya ait olduğunu doğrula
        private async Task<bool> IsEventOwner(Guid eventId, string userId)
        {
            return await _context.WeddingEvents.AnyAsync(e => e.Id == eventId && e.UserId == userId);
        }

        // Dashboard: List all events
        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var events = await _context.WeddingEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            
            if(events.Any())
            {
                var evt = events.First();
                ViewBag.EventName = evt.CoupleName;
                ViewBag.CurrentPlan = evt.PlanType;
                ViewBag.SalonName = evt.SalonName;
                ViewBag.LogoUrl = evt.LogoUrl;
                ViewBag.EventBaseUrl = GetEventBaseUrl();
                // Gerçek istatistikler
                ViewBag.TotalFiles = await _context.GuestEntries.CountAsync(e => e.EventId == evt.Id && e.IsApproved);
                ViewBag.PendingCount = await _context.GuestEntries.CountAsync(e => e.EventId == evt.Id && !e.IsApproved);
                ViewBag.TodayCount = await _context.GuestEntries.CountAsync(e => e.EventId == evt.Id && e.IsApproved && e.UploadedAt.Date == DateTime.Today);
                ViewBag.GuestCount = await _context.GuestEntries.Where(e => e.EventId == evt.Id && e.GuestName != null && e.GuestName != "").Select(e => e.GuestName!).Distinct().CountAsync();
            }
            else
            {
                ViewBag.CurrentPlan = dugunsalonu.Models.PlanType.Free;
            }
                
            return View(events);
        }

        // Create new event
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(WeddingEvent weddingEvent, IFormFile? logoFile)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    // Handle Logo Upload
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        string logoPath = await _fileService.SaveFileAsync(logoFile, "logos");
                        weddingEvent.LogoUrl = logoPath;
                    }
                }
                catch(Exception ex)
                {
                    ModelState.AddModelError("LogoUrl", "Logo yüklenirken hata oluştu: " + ex.Message);
                    return View(weddingEvent);
                }

                var slug = weddingEvent.CoupleName.ToLower();
                var trChars = new Dictionary<char, string> { 
                    {'ş', "s"}, {'ç', "c"}, {'ö', "o"}, {'ü', "u"}, {'ı', "i"}, {'ğ', "g"}, {' ', "-"}, {'&', "ve"} 
                };
                
                foreach (var chars in trChars) {
                    slug = slug.Replace(chars.Key.ToString(), chars.Value);
                }
                
                slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
                weddingEvent.Slug = slug;

                if (await _context.WeddingEvents.AnyAsync(e => e.Slug == weddingEvent.Slug))
                {
                    weddingEvent.Slug += "-" + Guid.NewGuid().ToString().Substring(0, 4);
                }

                var userId = await GetCurrentUserId();
                if (userId != null)
                {
                    weddingEvent.UserId = userId;
                }

                weddingEvent.CreatedAt = DateTime.Now;
                _context.Add(weddingEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(weddingEvent);
        }

        // Edit existing event
        [HttpGet]
        public async Task<IActionResult> EditEvent(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null) return NotFound();
            
            // Sahiplik kontrolü
            if (weddingEvent.UserId != userId) return Forbid();

            return View(weddingEvent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Guid id, WeddingEvent model, IFormFile? logoFile)
        {
            if (id != model.Id) return NotFound();

            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // Veritabanındaki orijinal kaydı çek
            var existingEvent = await _context.WeddingEvents.FindAsync(id);
            if (existingEvent == null) return NotFound();
            if (existingEvent.UserId != userId) return Forbid();

            // Formdan gelmeyen zorunlu alanların hatalarını temizle
            ModelState.Remove("Slug");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Logo Yükleme
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        // Eski logo varsa silinebilir (opsiyonel), yenisini yükle
                        string logoPath = await _fileService.SaveFileAsync(logoFile, "logos");
                        existingEvent.LogoUrl = logoPath;
                    }

                    // Diğer alanları güncelle
                    existingEvent.CoupleName = model.CoupleName;
                    existingEvent.EventDate = model.EventDate;
                    existingEvent.ThemeColor = model.ThemeColor;
                    existingEvent.SalonName = model.SalonName;
                    // Slug değiştirmiyoruz, linkler bozulmasın

                    _context.Update(existingEvent);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Etkinlik ayarları güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Hata: " + ex.Message);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQRCode(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null || weddingEvent.UserId != userId) return NotFound();

            // QR URL'sini oluştur
            var eventUrl = $"{Request.Scheme}://{Request.Host}/event/{weddingEvent.Slug}";

            // 1. QR Kodunu Oluştur
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(eventUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20); 

            using var qrImage = Image.Load(qrCodeBytes);

            // 2. Şık Bir Kart Oluştur (A4 oranına yakın: 1200x1600)
            int cardWidth = 1200;
            int cardHeight = 1600;
            using var card = new Image<Rgba32>(cardWidth, cardHeight);

            // Arka plan rengi (Açık bir tema rengi veya beyaz)
            var themeColor = Color.ParseHex(weddingEvent.ThemeColor ?? "#667eea");
            var bgColor = Color.White;

            card.Mutate(ctx => {
                ctx.Fill(bgColor);
                
                // Üst kısma hafif bir renk şeridi
                ctx.Fill(themeColor, new RectangularPolygon(0, 0, cardWidth, 200));

                // Font Ayarları
                FontFamily family;
                if (!SystemFonts.TryGet("Arial", out family)) {
                    family = SystemFonts.Families.First();
                }

                var titleFont = family.CreateFont(70, FontStyle.Bold);
                var subTitleFont = family.CreateFont(50, FontStyle.Regular);
                var namesFont = family.CreateFont(90, FontStyle.Bold);

                // Metin Seçenekleri (Ortalamak için)
                var centerOptions = new RichTextOptions(titleFont) {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Origin = new PointF(cardWidth / 2, 350)
                };

                // Başlık
                ctx.DrawText(centerOptions, "ANILARIMIZI BİZİMLE PAYLAŞIN", Color.Black);

                // Çift İsimleri
                centerOptions.Origin = new PointF(cardWidth / 2, 480);
                centerOptions.Font = namesFont;
                ctx.DrawText(centerOptions, weddingEvent.CoupleName.ToUpper(), themeColor);

                // QR Kodu Yerleştir
                int qrSize = 750;
                qrImage.Mutate(img => img.Resize(qrSize, qrSize));
                ctx.DrawImage(qrImage, new Point((cardWidth - qrSize) / 2, 650), 1f);

                // Alt Bilgi
                centerOptions.Origin = new PointF(cardWidth / 2, 1450);
                centerOptions.Font = subTitleFont;
                ctx.DrawText(centerOptions, "Kameranızla okutun ve fotoğraf yükleyin", Color.Gray);
                
                centerOptions.Origin = new PointF(cardWidth / 2, 1520);
                centerOptions.Font = family.CreateFont(30, FontStyle.Italic);
                ctx.DrawText(centerOptions, "Powered by KarWedding", Color.LightGray);
            });

            using var ms = new MemoryStream();
            await card.SaveAsPngAsync(ms);
            
            return File(ms.ToArray(), "image/png", $"{weddingEvent.Slug}-qr-davetiyesi.png");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAllPhotos(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null || weddingEvent.UserId != userId) return Forbid();

            var entries = await _context.GuestEntries
                .Where(e => e.EventId == id && e.PhotoPath != null)
                .ToListAsync();

            if (!entries.Any())
            {
                return Content("İndirilecek fotoğraf bulunamadı.");
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var entry in entries)
                    {
                        // Path /uploads/guests/filename.jpg -> Physical Path
                        var relativePath = entry.PhotoPath!.TrimStart('/');
                        var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

                        if (System.IO.File.Exists(physicalPath))
                        {
                            // Entry name: GuestName_UploadedAt_OriginalName
                            var fileName = Path.GetFileName(physicalPath);
                            var safeGuestName = string.Concat((entry.GuestName ?? "Misafir").Split(Path.GetInvalidFileNameChars()));
                            var entryName = $"{safeGuestName}_{entry.UploadedAt:yyyyMMdd_HHmmss}_{fileName}";
                            
                            archive.CreateEntryFromFile(physicalPath, entryName);
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipName = $"{weddingEvent.Slug}_tum_fotograflar_{DateTime.Now:yyyyMMdd}.zip";
                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }

        /// <summary>
        /// Onay bekleyen öğeleri JSON olarak döner (SignalR ile gerçek zamanlı ekleme için).
        /// </summary>
        [HttpGet("Admin/ModerationItems/{id}")]
        public async Task<IActionResult> ModerationItems(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null || weddingEvent.UserId != userId) return Forbid();

            var items = await _context.GuestEntries
                .Where(e => e.EventId == id && !e.IsApproved)
                .OrderBy(e => e.UploadedAt)
                .Select(e => new { e.Id, e.PhotoPath, e.Message, e.GuestName, UploadedAt = e.UploadedAt.ToString("HH:mm") })
                .ToListAsync();
            return Json(items);
        }

        // Moderation Page - Sahiplik kontrolü eklendi
        [HttpGet("Admin/Moderation/{id}")]
        public async Task<IActionResult> Moderation(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // Etkinliğin bu kullanıcıya ait olduğunu doğrula
            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null) return NotFound();
            if (weddingEvent.UserId != userId) return Forbid();

            var pendingPhotos = await _context.GuestEntries
                .Where(e => e.EventId == id && !e.IsApproved)
                .OrderBy(e => e.UploadedAt)
                .ToListAsync();

            ViewBag.EventName = weddingEvent.CoupleName;
            ViewBag.EventId = id;
            ViewBag.CurrentPlan = weddingEvent.PlanType;
            
            return View(pendingPhotos);
        }

        // Approve - Sahiplik kontrolü ve CSRF koruması eklendi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entry = await _context.GuestEntries.FindAsync(id);
            if (entry == null) return NotFound();

            // Bu entry'nin ait olduğu etkinliğin sahibi mi?
            if (!await IsEventOwner(entry.EventId, userId)) return Forbid();

            entry.IsApproved = true;
            await _context.SaveChangesAsync();
            
            var weddingEvent = await _context.WeddingEvents.FindAsync(entry.EventId);
            if (weddingEvent != null)
            {
                await _hubContext.Clients.Group(weddingEvent.Slug).SendAsync("ReceiveNewPhoto", entry.PhotoPath, entry.Message, entry.GuestName);
                var pendingCount = await _context.GuestEntries.CountAsync(e => e.EventId == entry.EventId && !e.IsApproved);
                await _adminHub.Clients.Group($"event-{entry.EventId}").SendAsync("NewPendingItems", pendingCount);
            }

            return Ok();
        }

        // Approve All - Sahiplik kontrolü
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAll(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null || weddingEvent.UserId != userId) return Forbid();

            var pendingEntries = await _context.GuestEntries
                .Where(e => e.EventId == id && !e.IsApproved)
                .ToListAsync();

            if (!pendingEntries.Any()) return Ok();

            foreach (var entry in pendingEntries)
            {
                entry.IsApproved = true;
                // SignalR ile slaytı güncelle
                await _hubContext.Clients.Group(weddingEvent.Slug).SendAsync("ReceiveNewPhoto", entry.PhotoPath, entry.Message, entry.GuestName);
            }

            await _context.SaveChangesAsync();
            
            // Admin paneli sayacını güncelle
            await _adminHub.Clients.Group($"event-{id}").SendAsync("NewPendingItems", 0);

            return Ok();
        }

        // Reject All - Sahiplik kontrolü
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAll(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var weddingEvent = await _context.WeddingEvents.FindAsync(id);
            if (weddingEvent == null || weddingEvent.UserId != userId) return Forbid();

            var pendingEntries = await _context.GuestEntries
                .Where(e => e.EventId == id && !e.IsApproved)
                .ToListAsync();

            if (!pendingEntries.Any()) return Ok();

            foreach (var entry in pendingEntries)
            {
                if (!string.IsNullOrEmpty(entry.GuestSessionId))
                {
                    await _guestHub.Clients.Group($"guest-{entry.GuestSessionId}").SendAsync("PhotoRejected");
                }
            }

            _context.GuestEntries.RemoveRange(pendingEntries);
            await _context.SaveChangesAsync();

            // Admin paneli sayacını güncelle
            await _adminHub.Clients.Group($"event-{id}").SendAsync("NewPendingItems", 0);

            return Ok();
        }

        // Reject - Sahiplik kontrolü ve CSRF koruması eklendi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entry = await _context.GuestEntries.FindAsync(id);
            if (entry == null) return NotFound();

            // Bu entry'nin ait olduğu etkinliğin sahibi mi?
            if (!await IsEventOwner(entry.EventId, userId)) return Forbid();

            // Misafire bildirim gönder
            if (!string.IsNullOrEmpty(entry.GuestSessionId))
            {
                await _guestHub.Clients.Group($"guest-{entry.GuestSessionId}").SendAsync("PhotoRejected");
            }

            var eventId = entry.EventId;
            _context.GuestEntries.Remove(entry);
            await _context.SaveChangesAsync();

            var pendingCount = await _context.GuestEntries.CountAsync(e => e.EventId == eventId && !e.IsApproved);
            await _adminHub.Clients.Group($"event-{eventId}").SendAsync("NewPendingItems", pendingCount);

            return Ok();
        }

        // Digital Album Gallery View for Admin
        public async Task<IActionResult> Album()
        {
            var userId = await GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var weddingEvent = await _context.WeddingEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.EventDate)
                .FirstOrDefaultAsync();

            if (weddingEvent == null) return RedirectToAction("Index");

            var photos = await _context.GuestEntries
                .Where(e => e.EventId == weddingEvent.Id && e.IsApproved)
                .OrderByDescending(e => e.UploadedAt)
                .ToListAsync();

            ViewBag.EventName = weddingEvent.CoupleName;
            ViewBag.CurrentPlan = weddingEvent.PlanType;

            return View(photos);
        }
    }
}
