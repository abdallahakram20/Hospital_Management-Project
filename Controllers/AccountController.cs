using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Hospital_Management_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Hospital_Management_Project.Controllers
{
    public class AccountController(AppDbContext context) : Controller
    {
        private readonly AppDbContext _context = context;

        // 1. عرض صفحة اللوجن
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // 2. معالجة بيانات اللوجن
        [HttpPost]
        [ValidateAntiForgeryToken]
        // تم تغيير LoginViewModel إلى CombinedLoginViewModel ليقرأ الحقول صح
        public async Task<IActionResult> Login(LoginView model)
        {
            try
            {
                if (model.UserType == "Staff")
                {
                    // تأكد أن موديل الـ Staff يحتوي على خاصية Email
                    var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Email == model.Email);

                    if (staff != null && BCrypt.Net.BCrypt.Verify(model.Password, staff.Password))
                    {
                        await SignInUser(staff.Email!, staff.Position ?? "Staff", staff.Fname + " " + staff.Lname);
                        return RedirectToAction("Index", "Home");
                    }
                }
                else if (model.UserType == "Patient")
                {
                    // تأكد أن موديل الـ Patient يحتوي على خاصية user_name
                    var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == model.UserName);

                    if (patient != null && BCrypt.Net.BCrypt.Verify(model.Password, patient.Password))
                    {
                        await SignInUser(patient.user_name!, "Patient", patient.FName + " " + patient.LName);
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                ModelState.AddModelError(string.Empty, "Account uses an old security format. Please reset your password.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        private async Task SignInUser(string identifier, string role, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, identifier),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}