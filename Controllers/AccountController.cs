using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using dugunsalonu.ViewModels;
using System.Security.Claims;
using dugunsalonu.Data;
using Microsoft.EntityFrameworkCore;

namespace dugunsalonu.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        private async Task<IActionResult> RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            
            // Check if user has any events
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var hasEvents = await _context.WeddingEvents.AnyAsync(e => e.UserId == user.Id);
                if (!hasEvents)
                {
                    return RedirectToAction("Welcome", "Onboarding");
                }
            }

            return RedirectToAction("Index", "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                // Handle error
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    var hasEvents = await _context.WeddingEvents.AnyAsync(e => e.UserId == user.Id);
                    if (!hasEvents)
                    {
                        return RedirectToAction("Welcome", "Onboarding");
                    }
                }
                return RedirectToAction("Index", "Admin");
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                // For simplicity here, we will auto-create the account using the email from Google.
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email != null)
                {
                    var user = new IdentityUser { UserName = email, Email = email };
                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        createResult = await _userManager.AddLoginAsync(user, info);
                        if (createResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            // New user has no events by definition
                            return RedirectToAction("Welcome", "Onboarding");
                        }
                    }
                }
                
                // If creation failed, redirect to login
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var hasEvents = await _context.WeddingEvents.AnyAsync(e => e.UserId == user.Id);
                        if (!hasEvents)
                        {
                            return RedirectToAction("Welcome", "Onboarding");
                        }
                    }

                    return RedirectToAction("Index", "Admin");
                }
                else if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Çok fazla hatalı giriş denemesi. Hesabınız 15 dakika süreyle kilitlendi.");
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Welcome", "Onboarding");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
