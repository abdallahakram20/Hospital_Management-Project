using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginView model)
        {
            // توحيد المعرف: إيميل لو موظف، واسم مستخدم لو مريض
            string loginIdentifier = model.UserType == "Staff" ? model.Email : model.UserName;
            loginIdentifier = loginIdentifier?.Trim().ToLower();

            if (string.IsNullOrEmpty(loginIdentifier))
            {
                ModelState.AddModelError(string.Empty, "Please enter your credentials.");
                return View(model);
            }

            // ✅ 1. فحص محاولات الدخول الفاشلة
            var failedAttempts = await _context.LoginAttempts
                .Where(la => la.Email.ToLower() == loginIdentifier &&
                              la.AttemptTime > DateTime.Now.AddMinutes(-15) &&
                             !la.Success)
                .CountAsync();

            if (failedAttempts >= 5)
            {
                ModelState.AddModelError("", "Account locked. Try again after 15 minutes");
                return View(model);
            }

            bool loginSuccess = false;

            try
            {
                // ... محاولة الدخول ...
                if (model.UserType == "Staff")
                {
                    var staff = await _context.Staff.FirstOrDefaultAsync(s => s.Email.ToLower() == loginIdentifier);

                    if (staff != null && BCrypt.Net.BCrypt.Verify(model.Password, staff.Password))
                    {
                        await SignInUser(
                            identifier: staff.Email!,
                            emailOrUsername: staff.Email!,
                            role: staff.Position ?? "Staff",
                            fullName: staff.Fname + " " + staff.Lname
                        );
                        loginSuccess = true;
                    }
                }
                else if (model.UserType == "Patient")
                {
                    var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name.ToLower() == loginIdentifier);

                    if (patient != null && BCrypt.Net.BCrypt.Verify(model.Password, patient.Password))
                    {
                        await SignInUser(
                            identifier: patient.user_name!,
                            emailOrUsername: patient.user_name!,
                            role: "Patient",
                            fullName: patient.FName + " " + patient.LName
                        );
                        loginSuccess = true;
                    }
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                ModelState.AddModelError(string.Empty, "Account uses an outdated security format. Please reset your password.");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during authentication.");
            }

            // ✅ 2. تسجيل المحاولة في الداتا بيز سواء نجحت أو فشلت
            var attempt = new LoginAttempt
            {
                Email = loginIdentifier, // بنخزن الإيميل أو اسم المستخدم
                AttemptTime = DateTime.Now,
                Success = loginSuccess
            };

            _context.LoginAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // ✅ 3. توجيه المستخدم لو العملية نجحت
            if (loginSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            // في حالة الفشل نضيف رسالة خطأ (لو مكنش فيه رسالة انضافت فوق)
            if (ModelState.ErrorCount == 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid authentication parameters or account not found.");
            }

            return View(model);
        }

        // Helper: بناء الـ Claims وعمل Sign In
        private async Task SignInUser(string identifier, string emailOrUsername, string role, string fullName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, identifier),
                new Claim(ClaimTypes.Email, emailOrUsername),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        // 3. POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // 4. GET: Account/AccessDenied
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

        // 9. POST: Account/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff(Staff staff)
        {
            var existingStaff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email.ToLower() == staff.Email.ToLower());

            if (existingStaff != null)
            {
                ModelState.AddModelError("Email", "This email is already registered");
                return View(staff);
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(staff.Password))
                {
                    staff.Password = BCrypt.Net.BCrypt.HashPassword(staff.Password);
                }

                _context.Add(staff);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Staff account created successfully!";
                return RedirectToAction("Index", "Staffs");
            }

            return View(staff);
        }
    }
}