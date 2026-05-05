using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hospital_Management_Project.Models;

namespace Hospital_Management_Project.Controllers
{
    public class Patient_Medical_ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public Patient_Medical_ProfileController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.PatientMedicalProfile.Include(p => p.Patient);
            return View(await appDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient_Medical_Profile = await _context.PatientMedicalProfile
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.ProfileId == id);
            if (patient_Medical_Profile == null)
            {
                return NotFound();
            }

            return View(patient_Medical_Profile);
        }

        public IActionResult Create()
        {
            var patientsWithProfiles = _context.PatientMedicalProfile.Select(p => p.PatientId).ToList();
            var availablePatients = _context.Patient
                .Where(p => !patientsWithProfiles.Contains(p.PatientId));

            ViewData["PatientId"] = new SelectList(availablePatients, "PatientId", "FName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                .Where(p => !patientsWithProfiles.Contains(p.PatientId) || p.PatientId == patient_Medical_Profile.PatientId);

            ViewData["PatientId"] = new SelectList(availablePatients, "PatientId", "FName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient_Medical_Profile = await _context.PatientMedicalProfile.FindAsync(id);
            if (patient_Medical_Profile == null)
            {
                return NotFound();
            }
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "FName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProfileId,Blood_Type,Blood_Pressure,Chronic_Disease,Allergies,Weight,Diabets,PatientId")] Patient_Medical_Profile patient_Medical_Profile)
        {
            if (id != patient_Medical_Profile.ProfileId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient_Medical_Profile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Patient_Medical_ProfileExists(patient_Medical_Profile.ProfileId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "FName", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient_Medical_Profile = await _context.PatientMedicalProfile
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.ProfileId == id);
            if (patient_Medical_Profile == null)
            {
                return NotFound();
            }

            return View(patient_Medical_Profile);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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