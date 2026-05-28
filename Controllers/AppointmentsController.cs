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
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff,Patient")]
        public IActionResult Create()
        {
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
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff,Patient")]
        public async Task<IActionResult> Create([Bind("AppointmentId,Visit_Date,Status,Reason,Diagnosis,Medication,Treatment_Plan,Notes,Common_tests,PatientId,StaffId")] Appointment appointment)
        {
            if (appointment.Visit_Date < DateTime.Now)
            {
                ModelState.AddModelError("Visit_Date", "Cannot create an appointment slot in a date/time that has already passed!");
            }

            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient")
            {
                var patientUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == patientUserName);

                if (patient != null)
                {
                    appointment.PatientId = patient.PatientId;
                    appointment.Status = "Booked";
                }
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(appointment.Status))
                {
                    appointment.Status = "Available";
                }

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

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
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff,Patient")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.Include(a => a.Staff).Include(a => a.Patient).FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Doctor"))
            {
                if (appointment.Staff?.Email != currentUserIdentifier)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            if (User.IsInRole("Patient"))
            {
                if (appointment.Patient?.user_name != currentUserIdentifier)
                {
                    return RedirectToAction("AccessDenied", "Account");
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

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Doctor,Receptionist,Staff,Patient")]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,Visit_Date,Status,Reason,Diagnosis,Medication,Treatment_Plan,Notes,Common_tests,PatientId,StaffId")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var originalAppointment = await _context.Appointment.AsNoTracking().Include(a => a.Staff).Include(a => a.Patient).FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (originalAppointment == null) return NotFound();

            if (User.IsInRole("Doctor") && originalAppointment.Staff?.Email != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Patient"))
            {
                if (originalAppointment.Patient?.user_name != currentUserIdentifier)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                appointment.Diagnosis = originalAppointment.Diagnosis;
                appointment.Medication = originalAppointment.Medication;
                appointment.Treatment_Plan = originalAppointment.Treatment_Plan;
                appointment.Notes = originalAppointment.Notes;
                appointment.Common_tests = originalAppointment.Common_tests;
                appointment.Status = originalAppointment.Status;
                appointment.PatientId = originalAppointment.PatientId;
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

        // =========================================================================
        //  دوال حجز وتعديل المواعيد للمريض
        // =========================================================================

        // GET: Appointments/BookAppointment/5
        [HttpGet]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            // فحص هل الموعد محجوز مسبقاً (تم تعديل الشرط ليتناسب مع int)
            if (appointment.PatientId > 0)
            {
                TempData["Error"] = "This appointment slot has already been booked!";
                return RedirectToAction("Details", "Staffs", new { id = appointment.StaffId });
            }

            return View(appointment);
        }

        // POST: Appointments/ConfirmBookAppointment
        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBookAppointment(int AppointmentId)
        {
            var appointment = await _context.Appointment.FindAsync(AppointmentId);
            if (appointment == null) return NotFound();

            if (appointment.Status != "Available" && appointment.Status != "متاح" && appointment.PatientId > 0)
            {
                TempData["Error"] = "This appointment slot has already been booked!";
                return RedirectToAction("Details", "Staffs", new { id = appointment.StaffId });
            }

            var patientUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == patientUserName);

            if (patient != null)
            {
                bool hasConflict = await _context.Appointment.AnyAsync(a =>
                    a.PatientId == patient.PatientId &&
                    a.Visit_Date == appointment.Visit_Date &&
                    a.AppointmentId != AppointmentId);

                if (hasConflict)
                {
                    TempData["Error"] = "Sorry, you already have another appointment booked at this exact date and time!";
                    return RedirectToAction("Details", "Staffs", new { id = appointment.StaffId });
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

            return RedirectToAction("Details", "Staffs", new { id = appointment.StaffId });
        }

        // GET: Appointments/ChangeAppointment/5
        [HttpGet]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ChangeAppointment(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            DateTime appointmentDateTime = appointment.Visit_Date;
            if ((appointmentDateTime - DateTime.Now).TotalHours <= 12)
            {
                TempData["Error"] = "Action denied! It is prohibited to change appointments with less than 12 hours remaining.";
                return RedirectToAction("Details", "Staffs", new { id = appointment.StaffId });
            }

            // تم تعديل الشرط هنا ليفحص الـ 0 بدلاً من null
            ViewData["AlternativeAppointments"] = await _context.Appointment
                .Where(a => a.StaffId == appointment.StaffId && a.PatientId == 0)
                .ToListAsync();

            return View(appointment);
        }

        // POST: Appointments/ConfirmChangeAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ConfirmChangeAppointment(int currentAppointmentId, int newAppointmentId)
        {
            var currentAppointment = await _context.Appointment.FindAsync(currentAppointmentId);
            var newAppointment = await _context.Appointment.FindAsync(newAppointmentId);

            if (currentAppointment == null || newAppointment == null) return NotFound();

            DateTime currentDateTime = currentAppointment.Visit_Date;
            if ((currentDateTime - DateTime.Now).TotalHours <= 12)
            {
                TempData["Error"] = "Action denied! Time lock is active (Less than 12 hours remaining).";
                return RedirectToAction("Details", "Staffs", new { id = currentAppointment.StaffId });
            }

            newAppointment.PatientId = currentAppointment.PatientId;
            newAppointment.Status = "Booked";

            // تم التعديل هنا: إسناد 0 بدلاً من null لحل خطأ الكومبايلر CS0037
            currentAppointment.PatientId = 0;
            currentAppointment.Status = "Available";

            _context.Update(currentAppointment);
            _context.Update(newAppointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your appointment has been successfully rescheduled to the new slot!";
            return RedirectToAction("Details", "Staffs", new { id = newAppointment.StaffId });
        }


        // GET: Appointments/PatientMedicalProfile/5
        public async Task<IActionResult> PatientMedicalProfile(int? id)
        {
            if (id == null) return NotFound();

            var medicalProfile = await _context.Set<Patient_Medical_Profile>()
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.ProfileId == id);

            if (medicalProfile == null) return NotFound();

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient" && medicalProfile.Patient?.user_name != currentUserIdentifier)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(medicalProfile);
        }

        // GET: Appointments/PrintReport
        public async Task<IActionResult> PrintReport()
        {
            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var appointmentsQuery = _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .AsQueryable();

            if (userRole == "Doctor")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Staff!.Email == currentUserIdentifier);
            }
            else if (userRole == "Patient")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Patient!.user_name == currentUserIdentifier);
            }

            var reportData = await appointmentsQuery.OrderByDescending(a => a.Visit_Date).ToListAsync();

            return View("PrintReport", reportData);
        }

        //                   CANCEL APPOINTMENT (Patient Only) 

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
                return NotFound();

            var currentUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.Patient?.user_name != currentUserName)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if ((appointment.Visit_Date - DateTime.Now).TotalHours <= 24)
            {
                TempData["Error"] = "لا يمكن إلغاء الموعد لأنه أقل من 24 ساعة متبقية على الموعد.";
                return RedirectToAction("Index");
            }

            appointment.Status = "Cancelled";
            appointment.Notes += $"\n[Cancelled by patient on {DateTime.Now:yyyy-MM-dd HH:mm}]";

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء الموعد بنجاح.";
            return RedirectToAction("Index");
        }
    }
}