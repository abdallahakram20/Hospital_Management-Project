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
    [Authorize] // Enforce authentication for all actions unless specified otherwise
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
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var patients = from p in _context.Patient select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(s => s.FName.Contains(searchString)
                                            || s.LName.Contains(searchString)
                                            || s.PatientId.ToString() == searchString);
            }

            return View(patients.ToList());
        }

        // GET: Patients/Details
        // Security: Medical staff can view any patient, but a patient can only view their own details

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patient.FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null) return NotFound();

            // Security Check: If user is a Patient, ensure they only access their own record

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
        // Available for Anonymous access so new patients can register

        [AllowAnonymous]
        public IActionResult Create()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
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
                // Ensure username uniqueness

                var exists = await _context.Patient.AnyAsync(p => p.user_name == patient.user_name);
                if (exists)
                {
                    ModelState.AddModelError("user_name", "This username is already taken. Please choose another one.");
                    return View(patient);
                }

                patient.Password = BCrypt.Net.BCrypt.HashPassword(patient.Password);
                _context.Add(patient);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login", "Account");
            }
            return View(patient);
        }

        // GET: Patients/Edit
        // Security: Only Admin/Staff or the Patient themselves can edit personal details

        public async Task<IActionResult> Edit(int? id)
        {
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
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,FName,LName,Address,Phone,Gender")] Patient patient)
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Protection: Retain old values for username and password so they don't get overwritten with null

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

                if (User.IsInRole("Patient"))
                    return RedirectToAction(nameof(Details), new { id = patient.PatientId });

                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patients/Delete

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
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
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patient.Any(e => e.PatientId == id);
        }

        // GET: Patients/ManageAccount/5
        // Account management page (Username & Password modification)
        public async Task<IActionResult> ManageAccount(int? id)
        {
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

        // POST: Patients/ManageAccount

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAccount(int id, string user_name, string Password)
        {
            var patient = await _context.Patient.FindAsync(id);
            if (patient == null) return NotFound();

            // Security Check

            if (User.IsInRole("Patient"))
            {
                var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (patient.user_name != currentUser)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            if (!string.IsNullOrEmpty(user_name))
            {
                // Validate Username uniqueness if changed

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

                // Smart Security Logic for Password Modifications

                if (User.IsInRole("Admin"))
                {
                    // Admin cannot write a custom password. Password resets to a secure default temporary value.

                    string temporaryPassword = "Default@Reset123";
                    patient.Password = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
                    TempData["Success"] = $"Username updated. Password has been reset to temporary default: {temporaryPassword}";
                }
                else if (User.IsInRole("Patient"))
                {
                    // The patient themselves must provide and update their custom password

                    if (!string.IsNullOrEmpty(Password))
                    {
                        patient.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                        TempData["Success"] = "Your account settings have been updated successfully.";
                    }
                    else
                    {
                        ModelState.AddModelError("Password", "Password is required.");
                        return View(patient);
                    }
                }

                _context.Update(patient);
                await _context.SaveChangesAsync();

                if (User.IsInRole("Patient"))
                    return RedirectToAction(nameof(Details), new { id = patient.PatientId });

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Username cannot be empty.");
            return View(patient);
        }
    }
}