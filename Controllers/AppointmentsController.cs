using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hospital_Management_Project.Controllers
{
    // ✅ Class for reading shift data from JSON file
    public class DoctorShiftDataDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // ✅ Method to read doctor shifts from JSON file
        // =======================================================
        private (TimeSpan Start, TimeSpan End) GetDoctorShiftFromFile(int staffId)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "doctor_shifts.json");
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                var shifts = JsonSerializer.Deserialize<Dictionary<int, DoctorShiftDataDto>>(json);
                if (shifts != null && shifts.TryGetValue(staffId, out var shift))
                {
                    return (shift.StartTime, shift.EndTime);
                }
            }
            // Default shift hours if not found (Morning shift default)
            return (new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0));
        }

        // =======================================================
        // ✅ Method to check availability (Used by AJAX in View)
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int staffId, DateTime visitDate)
        {
            bool isBooked = await _context.Appointment
                .AnyAsync(a => a.StaffId == staffId && a.Visit_Date == visitDate);

            return Json(new { isAvailable = !isBooked });
        }

        // =======================================================
        // ✅ Method to fetch available slots for a doctor on a specific day
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int staffId, DateTime date)
        {
            var shift = GetDoctorShiftFromFile(staffId);

            // Fetch booked appointments for this specific day
            var bookedAppointments = await _context.Appointment
                .Where(a => a.StaffId == staffId && a.Visit_Date.Date == date.Date)
                .ToListAsync();
            var bookedTimes = bookedAppointments.Select(a => a.Visit_Date.TimeOfDay).ToList();

            var availableSlots = new List<string>();
            var slotDuration = TimeSpan.FromMinutes(30); // Assuming consultation duration is 30 minutes

            var currentTime = shift.Start;
            var endTime = shift.End;

            // Handle shifts extending past midnight
            if (endTime <= currentTime) endTime = endTime.Add(TimeSpan.FromDays(1));

            while (currentTime < endTime)
            {
                var normalizedTime = currentTime.Days > 0 ? currentTime.Subtract(TimeSpan.FromDays(1)) : currentTime;

                if (!bookedTimes.Contains(normalizedTime))
                {
                    // ✅ Filter out past times if the requested date is today
                    if (date.Date == DateTime.Now.Date && normalizedTime <= DateTime.Now.TimeOfDay)
                    {
                        currentTime = currentTime.Add(slotDuration);
                        continue;
                    }

                    availableSlots.Add(normalizedTime.ToString(@"hh\:mm"));
                }
                currentTime = currentTime.Add(slotDuration);
            }

            return Json(availableSlots);
        }

        // =======================================================
        // ✅ Method to search patients by name
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> SearchPatients(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            term = term.ToLower();

            var patients = await _context.Patient
                .Where(p => p.FName.ToLower().Contains(term) || p.LName.ToLower().Contains(term))
                .Select(p => new
                {
                    id = p.PatientId,
                    name = p.FName + " " + p.LName
                })
                .Take(10)
                .ToListAsync();

            return Json(patients);
        }

        // =======================================================
        // ✅ Display appointments list (Index)
        // =======================================================
        public async Task<IActionResult> Index()
        {
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            var currentUserIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var appointmentsQuery = _context.Appointment.Include(a => a.Patient).Include(a => a.Staff).AsQueryable();

            if (userRole == "Doctor")
                appointmentsQuery = appointmentsQuery.Where(a => a.Staff!.Email == currentUserIdentifier);
            else if (userRole == "Patient")
                appointmentsQuery = appointmentsQuery.Where(a => a.Patient!.user_name == currentUserIdentifier);

            var appointmentsList = await appointmentsQuery.OrderByDescending(a => a.Visit_Date).ToListAsync();

            // ✅ تحديث حالة المواعيد ديناميكياً لتصبح Completed في الجدول إذا مر وقتها
            foreach (var appointment in appointmentsList)
            {
                if (appointment.Status == AppointmentStatus.Booked && appointment.Visit_Date < DateTime.Now)
                {
                    appointment.Status = AppointmentStatus.Completed;
                }
            }

            return View(appointmentsList);
        }

        // =======================================================
        // ✅ Open appointment details page (Details)
        // =======================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            // ✅ تحديث حالة الموعد لتصبح Completed في صفحة التفاصيل إذا مر وقته
            if (appointment.Status == AppointmentStatus.Booked && appointment.Visit_Date < DateTime.Now)
            {
                appointment.Status = AppointmentStatus.Completed;
            }

            return View(appointment);
        }

        // =======================================================
        // ✅ Add a new appointment (Create GET)
        // =======================================================
        public async Task<IActionResult> Create()
        {
            var patients = await _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToListAsync();
            var staffList = await _context.Staff.Include(s => s.Department).Where(s => s.Position == "Doctor").ToListAsync();

            var staff = staffList.Select(s => {
                var shift = GetDoctorShiftFromFile(s.StaffId);

                // ✅ Determine shift type based on start time
                string shiftType = "Emergency";
                if (shift.Start >= new TimeSpan(8, 0, 0) && shift.Start < new TimeSpan(16, 0, 0)) shiftType = "Morning";
                else if (shift.Start >= new TimeSpan(16, 0, 0)) shiftType = "Evening";

                return new
                {
                    s.StaffId,
                    FullName = $"{s.Fname} {s.Lname} {(s.Department != null ? $"({s.Department.DeptName})" : "")} [{shiftType} Shift: {shift.Start:hh\\:mm} to {shift.End:hh\\:mm}]"
                };
            }).ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName");
            ViewData["StaffId"] = new SelectList(staff, "StaffId", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,PatientId,StaffId,Visit_Date,Reason,Diagnosis,Medication,Common_tests,Treatment_Plan,Notes")] Appointment appointment)
        {
            // ✅ Prevent booking an appointment in the past
            if (appointment.Visit_Date < DateTime.Now)
            {
                ModelState.AddModelError("Visit_Date", "You cannot book an appointment in the past. Please select a valid future date and time.");
            }

            if (User.IsInRole("Patient"))
            {
                var currentUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentPatient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == currentUserName);
                if (currentPatient != null) appointment.PatientId = currentPatient.PatientId;
                ModelState.Remove("PatientId");
            }

            if (appointment.StaffId != null)
            {
                var time = appointment.Visit_Date.TimeOfDay;

                // ✅ Check if the time falls into the emergency period (From 12:00 AM to 07:59 AM)
                bool isEmergency = time >= TimeSpan.Zero && time < new TimeSpan(8, 0, 0);

                if (isEmergency)
                {
                    // Register as an emergency case and bypass regular shift checks
                    appointment.Reason = string.IsNullOrEmpty(appointment.Reason)
                        ? "[Emergency Case]"
                        : $"[Emergency Case] {appointment.Reason}";
                }
                else
                {
                    // 1. Validate shift hours for normal clinics
                    var shift = GetDoctorShiftFromFile((int)appointment.StaffId);
                    bool isWithinShift = (shift.Start <= shift.End)
                        ? (time >= shift.Start && time <= shift.End)
                        : (time >= shift.Start || time <= shift.End);

                    if (!isWithinShift)
                    {
                        ModelState.AddModelError("Visit_Date", $"The selected time is outside the doctor's shift hours ({shift.Start:hh\\:mm} to {shift.End:hh\\:mm}).");
                    }
                }

                // 2. Ensure no prior booking exists for the same slot (Applies to both Emergency and regular slots to avoid conflict)
                bool isAlreadyBooked = await _context.Appointment
                    .AnyAsync(a => a.StaffId == appointment.StaffId && a.Visit_Date == appointment.Visit_Date);

                if (isAlreadyBooked)
                {
                    ModelState.AddModelError("Visit_Date", "This appointment time is already booked for the selected doctor. Please choose another time.");
                }
            }

            if (ModelState.IsValid)
            {
                appointment.Status = AppointmentStatus.Booked; // Default status
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return RePopulateViewDataForEdit(appointment);
        }

        // =======================================================
        // ✅ Open edit appointment page (Edit GET)
        // =======================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            return RePopulateViewDataForEdit(appointment);
        }

        // =======================================================
        // ✅ Save edited appointment (Edit POST)
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,StaffId,Visit_Date,Reason,Status,Diagnosis,Medication,Treatment_Plan,Common_tests,Notes")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            if (appointment.StaffId != null)
            {
                var time = appointment.Visit_Date.TimeOfDay;

                // ✅ Check if the time falls into the emergency period
                bool isEmergency = time >= TimeSpan.Zero && time < new TimeSpan(8, 0, 0);

                if (isEmergency)
                {
                    // Ensure the emergency tag is preserved during update
                    if (string.IsNullOrEmpty(appointment.Reason) || (!appointment.Reason.Contains("[Emergency")))
                    {
                        appointment.Reason = string.IsNullOrEmpty(appointment.Reason)
                            ? "[Emergency Case]"
                            : $"[Emergency Case] {appointment.Reason}";
                    }
                }
                else
                {
                    // 1. Validate shift hours for normal clinics
                    var shift = GetDoctorShiftFromFile((int)appointment.StaffId);
                    bool isWithinShift = (shift.Start <= shift.End)
                        ? (time >= shift.Start && time <= shift.End)
                        : (time >= shift.Start || time <= shift.End);

                    if (!isWithinShift)
                    {
                        ModelState.AddModelError("Visit_Date", $"The selected time is outside the doctor's shift hours ({shift.Start:hh\\:mm} to {shift.End:hh\\:mm}).");
                    }
                }

                // 2. Ensure no prior booking exists for the same slot (excluding current appointment)
                bool isAlreadyBooked = await _context.Appointment
                    .AnyAsync(a => a.StaffId == appointment.StaffId && a.Visit_Date == appointment.Visit_Date && a.AppointmentId != appointment.AppointmentId);

                if (isAlreadyBooked)
                {
                    ModelState.AddModelError("Visit_Date", "This appointment time is already booked for the selected doctor. Please choose another time.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return RePopulateViewDataForEdit(appointment);
        }

        // =======================================================
        // ✅ Open delete appointment page (Delete GET)
        // =======================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            // ✅ Restrict patient from viewing delete page if less than 12 hours remain
            if (User.IsInRole("Patient"))
            {
                var timeRemaining = appointment.Visit_Date - DateTime.Now;
                if (timeRemaining.TotalHours < 12)
                {
                    TempData["Error"] = "There is less than 12 hours remaining before this appointment. You cannot cancel or delete it from your account. Please contact the hospital administration directly to request cancellation.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(appointment);
        }

        // =======================================================
        // ✅ Confirm appointment deletion (Delete POST)
        // =======================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment != null)
            {
                // ✅ Prevent direct POST requests if a patient tries to delete an appointment with less than 12 hours remaining
                if (User.IsInRole("Patient"))
                {
                    var timeRemaining = appointment.Visit_Date - DateTime.Now;
                    if (timeRemaining.TotalHours < 12)
                    {
                        TempData["Error"] = "There is less than 12 hours remaining before this appointment. You cannot cancel or delete it from your account. Please contact the hospital administration directly to request cancellation.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.Appointment.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // =======================================================
        // ✅ Appointment cancel action for patients
        // =======================================================
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            // ✅ Check if the logged-in user is a Patient and time left is less than 12 hours
            if (User.IsInRole("Patient"))
            {
                var timeRemaining = appointment.Visit_Date - DateTime.Now;
                if (timeRemaining.TotalHours < 12)
                {
                    TempData["Error"] = "There is less than 12 hours remaining before this appointment. You cannot cancel or delete it from your account. Please contact the hospital administration directly to request cancellation.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Change appointment status to cancelled
            appointment.Status = AppointmentStatus.Cancelled;

            _context.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment cancelled successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =======================================================
        // ✅ Helper method to populate dropdown lists
        // =======================================================
        private IActionResult RePopulateViewDataForEdit(Appointment appointment)
        {
            var pList = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();
            var sList = _context.Staff.Include(s => s.Department).Where(s => s.Position == "Doctor").ToList().Select(s => {
                var shift = GetDoctorShiftFromFile(s.StaffId);

                // ✅ Determine shift type based on start time here as well
                string shiftType = "Emergency";
                if (shift.Start >= new TimeSpan(8, 0, 0) && shift.Start < new TimeSpan(16, 0, 0)) shiftType = "Morning";
                else if (shift.Start >= new TimeSpan(16, 0, 0)) shiftType = "Evening";

                return new
                {
                    s.StaffId,
                    FullName = $"{s.Fname} {s.Lname} {(s.Department != null ? $"({s.Department.DeptName})" : "")} [{shiftType} Shift: {shift.Start:hh\\:mm} to {shift.End:hh\\:mm}]"
                };
            }).ToList();

            ViewData["PatientId"] = new SelectList(pList, "PatientId", "FullName", appointment.PatientId);
            ViewData["StaffId"] = new SelectList(sList, "StaffId", "FullName", appointment.StaffId);
            return View(appointment);
        }
    }
}