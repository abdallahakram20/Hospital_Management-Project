using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Hospital_Management_Project.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Hospital_Management_Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET: Account/Login
        // Displays the authentication portal interface

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // 2. POST: Account/Login
        // Processes identity verification inputs for both Staff and Patients

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView model)
        {
            try
            {
                if (model.UserType == "Staff")
                {
                    // Protection: Force validation evaluation against lowercase emails to ensure matching accuracy

                    var inputEmail = model.Email?.Trim().ToLower();
                    var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Email.ToLower() == inputEmail);

                    if (staff != null && BCrypt.Net.BCrypt.Verify(model.Password, staff.Password))
                    {
                        await SignInUser(staff.Email!, staff.Position ?? "Staff", staff.Fname + " " + staff.Lname);
                        return RedirectToAction("Index", "Home");
                    }
                }
                else if (model.UserType == "Patient")
                {
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
                ModelState.AddModelError(string.Empty, "Account uses an outdated security format. Please reset your password.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid authentication parameters or account not found.");
            return View(model);
        }

        // Helper Method: Establishes context principal claims and issues the encrypted security cookie wrapper
        private async Task SignInUser(string identifier, string role, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, identifier),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Security Policy: Inherit tracking parameters directly from Program.cs middleware setup (e.g., 20 mins sliding timeout)

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false, // Session clears completely upon browser termination alongside idle timeout
                AllowRefresh = true   // Permits automated cryptographic sliding renewal upon active client requests
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        // 3. POST: Account/Logout
        // Explicitly terminates current identity cookie sessions securely

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // 4. GET: Account/AccessDenied
        // Endpoint handler executed whenever unauthorized cross-role resource tampering is intercepted

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        // 5. GET: Account/ForgotPassword

        [HttpGet]
        public IActionResult ForgotPassword(string userType)
        {

            ViewBag.UserType = userType;
            return View();
        }

        // 6. POST: Account/ForgotPassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string identifier, string userType)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                ModelState.AddModelError(string.Empty, "Please enter your Email or Username.");
                ViewBag.UserType = userType;
                return View();
            }

            bool userExists = false;

            if (userType == "Staff")
            {
                var inputEmail = identifier.Trim().ToLower();
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Email.ToLower() == inputEmail);
                if (staff != null) userExists = true;
            }
            else if (userType == "Patient")
            {
                var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == identifier.Trim());
                if (patient != null) userExists = true;
            }

            if (userExists)
            {

                string resetToken = Guid.NewGuid().ToString();

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { token = resetToken, identifier = identifier, userType = userType },
                    protocol: HttpContext.Request.Scheme);

                ViewBag.ResetLink = resetLink;
            }

            ViewBag.Message = "If your account exists in our system, you will receive password reset instructions shortly.";

            return View("ForgotPasswordConfirmation");
        }

        // 7. GET: Account/ResetPassword
        // يعرض صفحة كتابة الباسورد الجديد
        [HttpGet]
        public IActionResult ResetPassword(string token, string identifier, string userType)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(identifier))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            ViewBag.Identifier = identifier;
            ViewBag.UserType = userType;

            return View();
        }

        // 8. POST: Account/ResetPassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string identifier, string userType, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                ViewBag.Identifier = identifier;
                ViewBag.UserType = userType;
                return View();
            }

            if (userType == "Staff")
            {
                var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Email == identifier);
                if (staff != null)
                {
                    staff.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    _context.Update(staff);
                }
            }
            else if (userType == "Patient")
            {
                var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == identifier);
                if (patient != null)
                {
                    patient.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    _context.Update(patient);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please login with your new password.";
            return RedirectToAction("Login");
        }
    }
}