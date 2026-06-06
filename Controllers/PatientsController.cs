using BCrypt.Net;
using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital_Management_Project.Controllers
{
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PatientsController : Controller
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        // Security: Patients are strictly prohibited from viewing the list of other patients
        [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse,Staff")]
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            // ✅ منع Back Button Loop
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            if (page < 1) page = 1;

            int pageSize = 20;
            ViewData["CurrentFilter"] = searchString;

            var patientsQuery = from p in _context.Patient select p;

            // =======================================================
            // ✅ التعديل الجديد: فلترة المرضى بناءً على الصلاحيات
            // =======================================================
            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Doctor")
            {
                // الطبيب يرى فقط المرضى الذين لديهم حجوزات معه
                var doctorPatientIds = _context.Appointment
                    .Where(a => a.Staff.Email == currentUserIdentifier)
                    .Select(a => a.PatientId)
                    .Distinct();

                patientsQuery = patientsQuery.Where(p => doctorPatientIds.Contains(p.PatientId));
            }
            // الأدمن وباقي الموظفين (Nurse, Receptionist, Staff) سيرون كل المرضى لأن الاستعلام لم يتغير لهم
            // =======================================================

            if (!string.IsNullOrEmpty(searchString))
            {
                patientsQuery = patientsQuery.Where(s => s.FName.Contains(searchString)
                                            || s.LName.Contains(searchString)
                                            || s.PatientId.ToString() == searchString);
            }

            int totalPatients = await patientsQuery.CountAsync();

            var patients = await patientsQuery
                .OrderBy(p => p.PatientId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalPatients / pageSize);
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < ViewBag.TotalPages;

            return View(patients);
        }

        // GET: Patients/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patient.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            if (User.IsInRole("Patient"))
            {
                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (patient.user_name != currentUser)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(patient);
        }

        // GET: Patients/PrintReport/5
        [Authorize(Roles = "Admin,Doctor,Receptionist,Nurse,Staff,Patient")]
        public async Task<IActionResult> PrintReport(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patient.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            if (User.IsInRole("Patient"))
            {
                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (patient.user_name != currentUser)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(patient);
        }

        // GET: Patients/Create
        [AllowAnonymous]
        public IActionResult Create()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Create([Bind("FName,LName,Address,Phone,Gender,user_name,Password")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.Patient.AnyAsync(p => p.user_name == patient.user_name);
                if (exists)
                {
                    ModelState.AddModelError("user_name", "This username is already taken. Please choose another one.");
                    return View(patient);
                }

                patient.Password = BCrypt.Net.BCrypt.HashPassword(patient.Password);
                _context.Add(patient);
                await _context.SaveChangesAsync();

                if (User.Identity != null && User.Identity.IsAuthenticated && !User.IsInRole("Patient"))
                {
                    // 🌟 تعديل توجيه الأدمن والموظفين بشكل صريح لجدول المرضى
                    return RedirectToAction("Index", "Patients");
                }

                return RedirectToAction("Login", "Account");
            }
            return View(patient);
        }

        // GET: Patients/Edit
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> Edit(int? id)
        {
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            if (id == null) return NotFound();

            var patient = await _context.Patient.FindAsync(id);
            if (patient == null) return NotFound();

            if (User.IsInRole("Patient"))
            {
                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (patient.user_name != currentUser)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(patient);
        }

        // POST: Patients/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Patient")]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,FName,LName,Address,Phone,Gender,user_name,Password")] Patient patient)
        {
            if (id != patient.PatientId) return NotFound();

            if (User.IsInRole("Patient"))
            {
                var currentPatient = await _context.Patient.FindAsync(id);
                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentPatient == null || currentPatient.user_name != currentUser)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            // ✅ نتجاهل validation الـ Password لأننا بنجيبه من الـ DB مش من الفورم
            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPatient = await _context.Patient.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == id);
                    if (existingPatient != null)
                    {
                        patient.user_name = existingPatient.user_name;
                        patient.Password = existingPatient.Password;
                    }

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId)) return NotFound();
                    else throw;
                }

                // 🌟 التعديل الجوهري لحل الـ 404:
                // المريض يتم توجيهه لصفحة تفاصيله الشخصية بدلاً من صفحة الـ Index المحجوبة عنه
                if (User.IsInRole("Patient"))
                {
                    return RedirectToAction("Details", "Patients", new { id = patient.PatientId });
                }

                // الأدمن يتم توجيهه بشكل صريح لجدول المرضى
                return RedirectToAction("Index", "Patients");
            }
            return View(patient);
        }

        // GET: Patients/Delete
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            if (id == null) return NotFound();

            var patient = await _context.Patient.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Patients/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patient.FindAsync(id);
            if (patient != null)
            {
                _context.Patient.Remove(patient);
            }

            await _context.SaveChangesAsync();

            // 🌟 تعديل توجيه الأدمن صراحةً بعد حذف المريض إلى قائمة المرضى
            return RedirectToAction("Index", "Patients");
        }

        private bool PatientExists(int id)
        {
            return _context.Patient.Any(e => e.PatientId == id);
        }

        // GET: Patients/ManageAccount/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageAccount(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patient.FindAsync(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Patients/ManageAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageAccount(int id, string user_name)
        {
            var patient = await _context.Patient.FindAsync(id);
            if (patient == null) return NotFound();

            if (!string.IsNullOrEmpty(user_name))
            {
                if (patient.user_name != user_name)
                {
                    var exists = await _context.Patient.AnyAsync(p => p.user_name == user_name);
                    if (exists)
                    {
                        ModelState.AddModelError("", "This username is already taken.");
                        return View(patient);
                    }
                }

                patient.user_name = user_name;
                TempData["Success"] = "Username updated successfully.";

                _context.Update(patient);
                await _context.SaveChangesAsync();

                // 🌟 تعديل توجيه صريح
                return RedirectToAction("Index", "Patients");
            }

            ModelState.AddModelError("", "Username cannot be empty.");
            return View(patient);
        }
    }
}