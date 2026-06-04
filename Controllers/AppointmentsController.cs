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
    // ✅ كلاس القراءة من ملف الـ JSON
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
        // ✅ دالة قراءة المواعيد من ملف الـ JSON
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
            // المواعيد الافتراضية
            return (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));
        }

        // =======================================================
        // ✅ دالة للتحقق من توافر الموعد (تُستخدم بواسطة AJAX في الـ View)
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int staffId, DateTime visitDate)
        {
            bool isBooked = await _context.Appointment
                .AnyAsync(a => a.StaffId == staffId && a.Visit_Date == visitDate);

            return Json(new { isAvailable = !isBooked });
        }

        // =======================================================
        // ✅ دالة لجلب المواعيد الفاضية للطبيب في يوم محدد
        // =======================================================
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(int staffId, DateTime date)
        {
            var shift = GetDoctorShiftFromFile(staffId);

            // جلب المواعيد المحجوزة في هذا اليوم
            var bookedAppointments = await _context.Appointment
                .Where(a => a.StaffId == staffId && a.Visit_Date.Date == date.Date)
                .ToListAsync();
            var bookedTimes = bookedAppointments.Select(a => a.Visit_Date.TimeOfDay).ToList();

            var availableSlots = new List<string>();
            var slotDuration = TimeSpan.FromMinutes(30); // افترضنا أن الكشف مدته 30 دقيقة

            var currentTime = shift.Start;
            var endTime = shift.End;

            // في حال كان الشفت يمتد لبعد منتصف الليل
            if (endTime <= currentTime) endTime = endTime.Add(TimeSpan.FromDays(1));

            while (currentTime < endTime)
            {
                var normalizedTime = currentTime.Days > 0 ? currentTime.Subtract(TimeSpan.FromDays(1)) : currentTime;

                if (!bookedTimes.Contains(normalizedTime))
                {
                    availableSlots.Add(normalizedTime.ToString(@"hh\:mm"));
                }
                currentTime = currentTime.Add(slotDuration);
            }

            return Json(availableSlots);
        }

        // =======================================================
        // ✅ دالة البحث عن المرضى بالاسم
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
        // ✅ عرض جدول المواعيد
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

            return View(await appointmentsQuery.OrderByDescending(a => a.Visit_Date).ToListAsync());
        }

        // =======================================================
        // ✅ فتح صفحة تفاصيل الموعد (Details) - كانت مفقودة
        // =======================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // =======================================================
        // ✅ إضافة موعد جديد
        // =======================================================
        public async Task<IActionResult> Create()
        {
            var patients = await _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToListAsync();
            var staffList = await _context.Staff.Include(s => s.Department).Where(s => s.Position == "Doctor").ToListAsync();

            var staff = staffList.Select(s => {
                var shift = GetDoctorShiftFromFile(s.StaffId);
                return new
                {
                    s.StaffId,
                    FullName = $"{s.Fname} {s.Lname} {(s.Department != null ? $"({s.Department.DeptName})" : "")} [Shift: {shift.Start:hh\\:mm} to {shift.End:hh\\:mm}]"
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
            if (User.IsInRole("Patient"))
            {
                var currentUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentPatient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == currentUserName);
                if (currentPatient != null) appointment.PatientId = currentPatient.PatientId;
                ModelState.Remove("PatientId");
            }

            if (appointment.StaffId != null)
            {
                // 1. التحقق من أوقات الشفت
                var shift = GetDoctorShiftFromFile((int)appointment.StaffId);
                var time = appointment.Visit_Date.TimeOfDay;
                bool isWithinShift = (shift.Start <= shift.End)
                    ? (time >= shift.Start && time <= shift.End)
                    : (time >= shift.Start || time <= shift.End);

                if (!isWithinShift)
                {
                    ModelState.AddModelError("Visit_Date", $"The selected time is outside the doctor's shift hours ({shift.Start:hh\\:mm} to {shift.End:hh\\:mm}).");
                }
                else
                {
                    // 2. التحقق من عدم وجود حجز مسبق لنفس الموعد
                    bool isAlreadyBooked = await _context.Appointment
                        .AnyAsync(a => a.StaffId == appointment.StaffId && a.Visit_Date == appointment.Visit_Date);

                    if (isAlreadyBooked)
                    {
                        ModelState.AddModelError("Visit_Date", "This appointment time is already booked for the selected doctor. Please choose another time.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                appointment.Status = AppointmentStatus.Booked; // الحالة الافتراضية
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return RePopulateViewDataForEdit(appointment);
        }

        // =======================================================
        // ✅ فتح صفحة تعديل الموعد (Edit GET) - كانت مفقودة
        // =======================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            return RePopulateViewDataForEdit(appointment);
        }

        // =======================================================
        // ✅ حفظ تعديل الموعد (Edit POST)
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,StaffId,Visit_Date,Reason,Status,Diagnosis,Medication,Treatment_Plan,Common_tests,Notes")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            if (appointment.StaffId != null)
            {
                // 1. التحقق من أوقات الشفت
                var shift = GetDoctorShiftFromFile((int)appointment.StaffId);
                var time = appointment.Visit_Date.TimeOfDay;
                bool isWithinShift = (shift.Start <= shift.End)
                    ? (time >= shift.Start && time <= shift.End)
                    : (time >= shift.Start || time <= shift.End);

                if (!isWithinShift)
                {
                    ModelState.AddModelError("Visit_Date", $"The selected time is outside the doctor's shift hours ({shift.Start:hh\\:mm} to {shift.End:hh\\:mm}).");
                }
                else
                {
                    // 2. التحقق من عدم وجود حجز مسبق لنفس الموعد (مع استثناء الموعد الحالي من المقارنة)
                    bool isAlreadyBooked = await _context.Appointment
                        .AnyAsync(a => a.StaffId == appointment.StaffId && a.Visit_Date == appointment.Visit_Date && a.AppointmentId != appointment.AppointmentId);

                    if (isAlreadyBooked)
                    {
                        ModelState.AddModelError("Visit_Date", "This appointment time is already booked for the selected doctor. Please choose another time.");
                    }
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
        // ✅ فتح صفحة حذف الموعد (Delete GET) - كانت مفقودة
        // =======================================================
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

        // =======================================================
        // ✅ تأكيد حذف الموعد (Delete POST) - كانت مفقودة
        // =======================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointment.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // =======================================================
        // ✅ دالة مساعدة لتعبئة بيانات القوائم المنسدلة (Dropdowns)
        // =======================================================
        private IActionResult RePopulateViewDataForEdit(Appointment appointment)
        {
            var pList = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();
            var sList = _context.Staff.Include(s => s.Department).Where(s => s.Position == "Doctor").ToList().Select(s => {
                var shift = GetDoctorShiftFromFile(s.StaffId);
                return new
                {
                    s.StaffId,
                    FullName = $"{s.Fname} {s.Lname} {(s.Department != null ? $"({s.Department.DeptName})" : "")} [Shift: {shift.Start:hh\\:mm} to {shift.End:hh\\:mm}]"
                };
            }).ToList();

            ViewData["PatientId"] = new SelectList(pList, "PatientId", "FullName", appointment.PatientId);
            ViewData["StaffId"] = new SelectList(sList, "StaffId", "FullName", appointment.StaffId);
            return View(appointment);
        }
    }
}