using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dugunsalonu.Models;
using dugunsalonu.Data;
using dugunsalonu.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace dugunsalonu.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private static readonly string[] ValidPlans = { "Pro", "SalonBusiness" };

        // GET: /Payment/Checkout?plan=Pro
        [HttpGet]
        public IActionResult Checkout(string plan)
        {
            if (string.IsNullOrEmpty(plan) || !ValidPlans.Contains(plan))
            {
                return RedirectToAction("Index", "Home", new { anchor = "fiyatlandirma" });
            }

            var model = new PaymentViewModel { Plan = plan };
            return View(model);
        }

        // POST: /Payment/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            if (!ValidPlans.Contains(model.Plan))
            {
                return RedirectToAction("Index", "Home", new { anchor = "fiyatlandirma" });
            }

            // Server-side card validation
            string cleanCardNumber = model.CardNumber.Replace(" ", "");
            if (cleanCardNumber.Length < 15 || cleanCardNumber.Length > 16 || !cleanCardNumber.All(char.IsDigit))
            {
                ModelState.AddModelError("CardNumber", "Geçersiz kart numarası.");
                return View("Checkout", model);
            }

            // Luhn algorithm check
            if (!IsValidLuhn(cleanCardNumber))
            {
                ModelState.AddModelError("CardNumber", "Geçersiz kart numarası.");
                return View("Checkout", model);
            }

            // Check expiry date
            if (!IsValidExpiryDate(model.ExpiryDate))
            {
                ModelState.AddModelError("ExpiryDate", "Kartınızın son kullanma tarihi geçmiş.");
                return View("Checkout", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // ============================================================
                // ÖDEME İŞLEMİ
                // Gerçek projede burada ödeme gateway entegrasyonu olacak
                // (iyzico, Stripe, PayTR vb.)
                // Şu an demo olarak simüle ediyoruz
                // ============================================================

                if (!Enum.TryParse<PlanType>(model.Plan, true, out var newPlan))
                {
                    ModelState.AddModelError("", "Geçersiz plan tipi.");
                    return View("Checkout", model);
                }

                // Find user's events and upgrade
                var userEvents = await _context.WeddingEvents
                    .Where(e => e.UserId == user.Id)
                    .ToListAsync();

                if (!userEvents.Any())
                {
                    // User has no events yet - store the plan upgrade for later
                    // or show a message to create an event first
                    TempData["PendingPlan"] = model.Plan;
                }

                foreach (var evt in userEvents)
                {
                    evt.PlanType = newPlan;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Payment processed successfully. User: {UserId}, Plan: {Plan}, Amount: {Amount} TL",
                    user.Id, model.Plan, model.GetPriceRaw());

                // Redirect to success page
                return RedirectToAction("Success", new { plan = model.Plan });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed for user {UserId}", user.Id);
                ModelState.AddModelError("", "Ödeme işlemi sırasında bir hata oluştu. Lütfen tekrar deneyin.");
                return View("Checkout", model);
            }
        }

        // GET: /Payment/Success?plan=Pro
        [HttpGet]
        public async Task<IActionResult> Success(string plan)
        {
            if (string.IsNullOrEmpty(plan) || !ValidPlans.Contains(plan))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            bool hasEvents = false;
            if (user != null)
            {
                hasEvents = await _context.WeddingEvents.AnyAsync(e => e.UserId == user.Id);
            }

            // Keep PendingPlan alive for onboarding flow
            if (TempData.ContainsKey("PendingPlan"))
            {
                TempData.Keep("PendingPlan");
            }

            ViewBag.Plan = plan;
            ViewBag.Price = PlanConfig.GetPrice(plan);
            ViewBag.HasEvents = hasEvents;
            return View();
        }

        // Luhn algorithm for card number validation
        private static bool IsValidLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(number[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        // Expiry date validation
        private static bool IsValidExpiryDate(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate)) return false;

            var parts = expiryDate.Split('/');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
                return false;

            if (month < 1 || month > 12) return false;

            year += 2000; // Convert YY to YYYY
            var expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            return expiry >= DateTime.Now;
        }
    }
}
