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
    [Authorize] // Enforce login for all users across all actions unless specified otherwise
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        // Security Logic: Filter appointments based on the logged-in user's role

        public async Task<IActionResult> Index()
        {
            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var appointmentsQuery = _context.Appointment.Include(a => a.Patient).Include(a => a.Staff).AsQueryable();

            // 1. If Doctor: View only their own assigned appointments via linked Email

            if (userRole == "Doctor")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Staff!.Email == currentUserIdentifier);
            }

            // 2. If Patient: View only their personal booked appointments

            else if (userRole == "Patient")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Patient!.user_name == currentUserIdentifier);
            }

            // 3. If Admin or Receptionist: Can see and manage all appointments

            return View(await appointmentsQuery.ToListAsync());
        }

        // GET: Appointments/Details/5
        // Security: Prevent patients or doctors from unauthorized spying on other users' appointment details

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient" && appointment.Patient?.user_name != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            if (userRole == "Doctor" && appointment.Staff?.Email != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        // Security: Patients are strictly forbidden from creating empty appointments from scratch

        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff")]
        public IActionResult Create()
        {
            // Filter out Admins and construct the Full Name structure safely

            var filteredDoctors = _context.Staff
                .Where(s => s.Position != "Admin")
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    FullName = s.Fname + " " + s.Lname
                })
                .ToList();

            ViewData["StaffList"] = new SelectList(filteredDoctors, "StaffId", "FullName");
            return View();
        }

        // POST: Appointments/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff")]
        public async Task<IActionResult> Create([Bind("AppointmentId,Visit_Date,Status,Reason,Diagnosis,Medication,Treatment_Plan,Notes,Common_tests,PatientId,StaffId")] Appointment appointment)
        {
            // Guard Clause: Prevent creating an appointment slots with a past date and time

            if (appointment.Visit_Date < DateTime.Now)
            {
                ModelState.AddModelError("Visit_Date", "Cannot create an appointment slot in a date/time that has already passed!");
            }

            if (ModelState.IsValid)
            {
                // Set default status if empty

                if (string.IsNullOrEmpty(appointment.Status))
                {
                    appointment.Status = "Available";
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate filtered list in case of validation failures

            var filteredDoctors = _context.Staff
                .Where(s => s.Position != "Admin")
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    FullName = s.Fname + " " + s.Lname
                })
                .ToList();

            ViewData["StaffList"] = new SelectList(filteredDoctors, "StaffId", "FullName", appointment.StaffId);
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        // Security: Restrict access to editing diagnoses, prescriptions, and slot details

        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.Include(a => a.Staff).FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            // Security Check: Ensure editing doctor is the actual owner of this specific appointment slot

            if (User.IsInRole("Doctor"))
            {
                var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (appointment.Staff?.Email != currentUserEmail)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var patients = _context.Patient
                .Select(p => new { id = p.PatientId, name = p.FName + " " + p.LName })
                .ToList();

            // Re-filtered Staff Dropdown during edit cycles to keep UI sanitized

            var filteredDoctors = _context.Staff
                .Where(s => s.Position != "Admin")
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    FullName = s.Fname + " " + s.Lname
                })
                .ToList();

            ViewData["Patients"] = new SelectList(patients, "id", "name", appointment.PatientId);
            ViewData["StaffList"] = new SelectList(filteredDoctors, "StaffId", "FullName", appointment.StaffId);

            return View(appointment);
        }

        // POST: Appointments/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,Visit_Date,Status,Reason,Diagnosis,Medication,Treatment_Plan,Notes,Common_tests,PatientId,StaffId")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            // Security POST Verification: Prevent cross-parameter parameter tampering attacks on appointment IDs

            if (User.IsInRole("Doctor"))
            {
                var originalAppointment = await _context.Appointment.AsNoTracking().Include(a => a.Staff).FirstOrDefaultAsync(a => a.AppointmentId == id);
                var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (originalAppointment == null || originalAppointment.Staff?.Email != currentUserEmail)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId)) return NotFound();
                    else throw;
                }
            }

            var patients = _context.Patient
                .Select(p => new { id = p.PatientId, name = p.FName + " " + p.LName })
                .ToList();

            var filteredDoctors = _context.Staff
                .Where(s => s.Position != "Admin")
                .Select(s => new
                {
                    StaffId = s.StaffId,
                    FullName = s.Fname + " " + s.Lname
                })
                .ToList();

            ViewData["Patients"] = new SelectList(patients, "id", "name", appointment.PatientId);
            ViewData["StaffList"] = new SelectList(filteredDoctors, "StaffId", "FullName", appointment.StaffId);

            return View(appointment);
        }

        // GET: Appointments/Delete/5

        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Receptionist")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointment.Remove(appointment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointment.Any(e => e.AppointmentId == id);
        }

        // GET: Appointments/SearchPatients

        [HttpGet]
        public JsonResult SearchPatients(string term)
        {
            var patients = _context.Patient
                .Where(p => (p.FName + " " + p.LName).Contains(term) || p.FName.Contains(term) || p.LName.Contains(term))
                .Select(p => new { id = p.PatientId, name = p.FName + " " + p.LName })
                .Take(10)
                .ToList();
            return Json(patients);
        }

        // POST: Appointments/BookAppointment
        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(int appointmentId)
        {
            var appointment = await _context.Appointment.FindAsync(appointmentId);
            if (appointment == null) return NotFound();

            if (appointment.Status != "Available" && appointment.Status != "متاح")
            {
                TempData["Error"] = "This appointment slot has already been booked!";
                return RedirectToAction("Index", "Staffs");
            }

            var patientUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == patientUserName);

            if (patient != null)
            {
                // Double Booking Prevention: Check if the patient already has an active appointment at this exact date and time slot

                bool hasConflict = await _context.Appointment.AnyAsync(a =>
                    a.PatientId == patient.PatientId &&
                    a.Visit_Date == appointment.Visit_Date &&
                    a.AppointmentId != appointmentId);

                if (hasConflict)
                {
                    TempData["Error"] = "Sorry, you already have another appointment booked at this exact date and time!";
                    return RedirectToAction("Index", "Staffs");
                }

                appointment.PatientId = patient.PatientId;
                appointment.Status = "Booked";

                _context.Update(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "The appointment has been successfully booked!";
            }
            else
            {
                TempData["Error"] = "Patient profile data could not be found.";
            }

            return RedirectToAction("Index", "Staffs");
        }
    }
}