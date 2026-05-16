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
    }
}