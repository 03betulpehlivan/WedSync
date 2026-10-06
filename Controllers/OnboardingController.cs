using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dugunsalonu.Models;
using dugunsalonu.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Collections.Generic;

namespace dugunsalonu.Controllers
{
    [Authorize]
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OnboardingController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Welcome()
        {
            // Keep PendingPlan alive through onboarding flow
            if (TempData.ContainsKey("PendingPlan"))
                TempData.Keep("PendingPlan");
            return View();
        }

        public IActionResult SelectType()
        {
            if (TempData.ContainsKey("PendingPlan"))
                TempData.Keep("PendingPlan");
            return View();
        }
        
        public IActionResult EventDetails(string type)
        {
            if (string.IsNullOrEmpty(type)) return RedirectToAction("SelectType");
            if (TempData.ContainsKey("PendingPlan"))
                TempData.Keep("PendingPlan");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteSetup(string coupleName, DateTime eventDate, string themeColor)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Slug generation logic (similar to AdminController)
            var slug = coupleName.ToLower();
            var trChars = new Dictionary<char, string> { 
                {'ş', "s"}, {'ç', "c"}, {'ö', "o"}, {'ü', "u"}, {'ı', "i"}, {'ğ', "g"}, {' ', "-"}, {'&', "ve"} 
            };
            foreach (var chars in trChars) {
                slug = slug.Replace(chars.Key.ToString(), chars.Value);
            }
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Ensure unique slug
            if (_context.WeddingEvents.Any(e => e.Slug == slug))
            {
                slug += "-" + Guid.NewGuid().ToString().Substring(0, 4);
            }

            // Check if user has a pending plan from payment
            var pendingPlan = PlanType.Free;
            if (TempData["PendingPlan"] is string pendingPlanStr)
            {
                if (Enum.TryParse<PlanType>(pendingPlanStr, true, out var parsedPlan))
                {
                    pendingPlan = parsedPlan;
                }
            }

            var newEvent = new WeddingEvent
            {
                Id = Guid.NewGuid(),
                CoupleName = coupleName,
                EventDate = eventDate,
                ThemeColor = themeColor ?? "#FFFFFF",
                Slug = slug,
                UserId = user.Id,
                PlanType = pendingPlan,
                CreatedAt = DateTime.Now
            };

            _context.WeddingEvents.Add(newEvent);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Admin");
        }

        // DEVELOPMENT ONLY: Reset user state to test onboarding again
        public async Task<IActionResult> Reset([FromServices] IWebHostEnvironment env)
        {
            // Bu endpoint sadece development ortamında çalışır
            if (!env.IsDevelopment())
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var events = _context.WeddingEvents.Where(e => e.UserId == user.Id);
                _context.WeddingEvents.RemoveRange(events);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Welcome");
        }
    }
}
