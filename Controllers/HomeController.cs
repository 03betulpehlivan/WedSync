using System.Diagnostics;
using dugunsalonu.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace dugunsalonu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly dugunsalonu.Data.ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, dugunsalonu.Data.ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Ensure demo event exists for the landing page button
            var demoEvent = _context.WeddingEvents.FirstOrDefault(e => e.Slug == "ahmet-ayse");
            if (demoEvent == null)
            {
                demoEvent = new dugunsalonu.Models.WeddingEvent
                {
                    CoupleName = "Ahmet & Ayşe",
                    EventDate = DateTime.Now,
                    Slug = "ahmet-ayse",
                    ThemeColor = "#FF5A5F",
                    CreatedAt = DateTime.Now
                };
                _context.WeddingEvents.Add(demoEvent);
                _context.SaveChanges();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
