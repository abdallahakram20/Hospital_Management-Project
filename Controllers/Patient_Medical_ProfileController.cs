using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hospital_Management_Project.Controllers
{
    [Authorize] // Enforce authentication across the entire controller
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class Patient_Medical_ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public Patient_Medical_ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Patient_Medical_Profile
        // Security: Patients can only see their own profile. Doctors/Admins see all records.
        public async Task<IActionResult> Index()
        {
            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var profilesQuery = _context.PatientMedicalProfile.Include(p => p.Patient).AsQueryable();

            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                // Filter to only fetch the logged-in patient's record based on username
                profilesQuery = profilesQuery.Where(p => p.Patient!.user_name == currentUserIdentifier);
            }

            return View(await profilesQuery.ToListAsync());
        }

        // GET: Patient_Medical_Profile/Details/5
        // Security: Prevent cross-patient spying on medical details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient_Medical_Profile = await _context.PatientMedicalProfile
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.ProfileId == id);

            if (patient_Medical_Profile == null) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Bouncer Check: If patient, block access and redirect to AccessDenied if the record doesn't belong to them
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase) &&
                patient_Medical_Profile.Patient?.user_name != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(patient_Medical_Profile);
        }

        // ====================================================================
        // GET: Patient_Medical_Profile/PrintReport/5
        // Security: Same as Details, Patients can only print their own records
        // ====================================================================
        public async Task<IActionResult> PrintReport(int? id)
        {
            if (id == null) return NotFound();

            var patient_Medical_Profile = await _context.PatientMedicalProfile
                .Include(p => p.Patient) // Ensure Patient data is loaded if needed in the print view
                .FirstOrDefaultAsync(m => m.ProfileId == id);

            if (patient_Medical_Profile == null) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Security Check: Block patient if they try to print someone else's report
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase) &&
                patient_Medical_Profile.Patient?.user_name != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            return View(patient_Medical_Profile);
        }

        // GET: Patient_Medical_Profile/Create
        // Security Roles: Patients are strictly forbidden from authoring medical records
        [Authorize(Roles = "Admin,Doctor,Staff")]
        public IActionResult Create()
        {
            var patientsWithProfiles = _context.PatientMedicalProfile.Select(p => p.PatientId).ToList();

            // Refactored dropdown to concatenate full name cleanly
            var availablePatients = _context.Patient
                .Where(p => !patientsWithProfiles.Contains(p.PatientId))
                .Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName })
                .ToList();

            ViewData["PatientId"] = new SelectList(availablePatients, "PatientId", "FullName");
            return View();
        }

        // POST: Patient_Medical_Profile/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor,Staff")]
        public async Task<IActionResult> Create([Bind("ProfileId,Blood_Type,Blood_Pressure,Chronic_Disease,Allergies,Weight,Diabets,PatientId")] Patient_Medical_Profile patient_Medical_Profile)
        {
            bool profileExists = await _context.PatientMedicalProfile
                .AnyAsync(p => p.PatientId == patient_Medical_Profile.PatientId);

            if (profileExists)
            {
                ModelState.AddModelError("PatientId", "This patient already has a medical profile.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient_Medical_Profile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var patientsWithProfiles = _context.PatientMedicalProfile.Select(p => p.PatientId).ToList();
            var availablePatients = _context.Patient
                .Where(p => !patientsWithProfiles.Contains(p.PatientId) || p.PatientId == patient_Medical_Profile.PatientId)
                .Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName })
                .ToList();

            ViewData["PatientId"] = new SelectList(availablePatients, "PatientId", "FullName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // GET: Patient_Medical_Profile/Edit
        // Security Roles: Patients cannot edit their own medical profile data
        [Authorize(Roles = "Admin,Doctor,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var patient_Medical_Profile = await _context.PatientMedicalProfile.FindAsync(id);
            if (patient_Medical_Profile == null) return NotFound();

            var patients = _context.Patient
                .Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName })
                .ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // POST: Patient_Medical_Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("ProfileId,Blood_Type,Blood_Pressure,Chronic_Disease,Allergies,Weight,Diabets,PatientId")] Patient_Medical_Profile patient_Medical_Profile)
        {
            if (id != patient_Medical_Profile.ProfileId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient_Medical_Profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Patient_Medical_ProfileExists(patient_Medical_Profile.ProfileId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var patients = _context.Patient
                .Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName })
                .ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // GET: Patient_Medical_Profile/Delete
        // Security: Block Patients from executing deletions, redirecting instantly to AccessDenied
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Strict check: If user role resolves to Patient, boot them out completely
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var patient_Medical_Profile = await _context.PatientMedicalProfile
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.ProfileId == id);
            if (patient_Medical_Profile == null) return NotFound();

            return View(patient_Medical_Profile);
        }

        // POST: Patient_Medical_Profile/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient_Medical_Profile = await _context.PatientMedicalProfile.FindAsync(id);
            if (patient_Medical_Profile != null)
            {
                _context.PatientMedicalProfile.Remove(patient_Medical_Profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Patient_Medical_ProfileExists(int id)
        {
            return _context.PatientMedicalProfile.Any(e => e.ProfileId == id);
        }
    }
}