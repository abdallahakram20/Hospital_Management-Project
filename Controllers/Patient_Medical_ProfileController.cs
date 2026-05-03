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

        // GET: Patient_Medical_Profile
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.PatientMedicalProfile.Include(p => p.Patient);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Patient_Medical_Profile/Details/5
        public async Task<IActionResult> Details(string id)
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

        // GET: Patient_Medical_Profile/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "PatientId");
            return View();
        }

        // POST: Patient_Medical_Profile/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProfileId,Blood_Type,Blood_Pressure,Chronic_Disease,Allergies,Weight,Diabets,PatientId")] Patient_Medical_Profile patient_Medical_Profile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(patient_Medical_Profile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "PatientId", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // GET: Patient_Medical_Profile/Edit/5
        public async Task<IActionResult> Edit(string id)
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
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "PatientId", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // POST: Patient_Medical_Profile/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ProfileId,Blood_Type,Blood_Pressure,Chronic_Disease,Allergies,Weight,Diabets,PatientId")] Patient_Medical_Profile patient_Medical_Profile)
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
            ViewData["PatientId"] = new SelectList(_context.Patient, "PatientId", "PatientId", patient_Medical_Profile.PatientId);
            return View(patient_Medical_Profile);
        }

        // GET: Patient_Medical_Profile/Delete/5
        public async Task<IActionResult> Delete(string id)
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

        // POST: Patient_Medical_Profile/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var patient_Medical_Profile = await _context.PatientMedicalProfile.FindAsync(id);
            if (patient_Medical_Profile != null)
            {
                _context.PatientMedicalProfile.Remove(patient_Medical_Profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Patient_Medical_ProfileExists(string id)
        {
            return _context.PatientMedicalProfile.Any(e => e.ProfileId == id);
        }
    }
}
