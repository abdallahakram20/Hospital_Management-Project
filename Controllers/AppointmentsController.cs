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

            if (userRole == "Doctor")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Staff!.Email == currentUserIdentifier);
            }
            else if (userRole == "Patient")
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Patient!.user_name == currentUserIdentifier);
            }

            return View(await appointmentsQuery.OrderByDescending(a => a.Visit_Date).ToListAsync());
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

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            var patients = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

            // 🌟 التعديل هنا: جلب الأطباء فقط بالاعتماد على حقل Position
            var staff = _context.Staff
                .Where(s => s.Position == "Doctor") // نجلب من وظيفته طبيب فقط
                .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                .ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName");
            ViewData["StaffId"] = new SelectList(staff, "StaffId", "FullName");
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,PatientId,StaffId,Visit_Date,Reason,Diagnosis,Medication,Common_tests,Treatment_Plan,Notes")] Appointment appointment)
        {
            // 🌟 الحل هنا: تعيين الـ PatientId تلقائياً للمريض بناءً على حسابه المفتوح
            if (User.IsInRole("Patient"))
            {
                var currentUserName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentPatient = await _context.Patient.FirstOrDefaultAsync(p => p.user_name == currentUserName);

                if (currentPatient != null)
                {
                    appointment.PatientId = currentPatient.PatientId;
                }

                // إزالة الحقل من التحقق حتى لا يعترض الـ ModelState إذا كان فارغاً من شاشة المريض
                ModelState.Remove("PatientId");
            }

            if (ModelState.IsValid)
            {
                appointment.Status = AppointmentStatus.Booked;

                _context.Add(appointment);
                await _context.SaveChangesAsync(); // الآن سيتم الحفظ بنجاح دون أخطاء الـ Foreign Key
                TempData["Success"] = "Appointment created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // إعادة تعبئة القوائم في حال فشل التحقق (Validation)
            var patients = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

            var staff = _context.Staff
                .Where(s => s.Position == "Doctor")
                .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                .ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName", appointment.PatientId);
            ViewData["StaffId"] = new SelectList(staff, "StaffId", "FullName", appointment.StaffId);
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment == null) return NotFound();

            var patients = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

            var staff = _context.Staff
                .Where(s => s.Position == "Doctor")
                .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                .ToList();

            ViewData["PatientId"] = new SelectList(patients, "PatientId", "FullName", appointment.PatientId);
            ViewData["StaffId"] = new SelectList(staff, "StaffId", "FullName", appointment.StaffId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,StaffId,Visit_Date,Reason,Status,Diagnosis,Medication,Treatment_Plan,Common_tests,Notes")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var originalAppointment = await _context.Appointment.AsNoTracking().FirstOrDefaultAsync(a => a.AppointmentId == id);
                    if (originalAppointment == null) return NotFound();

                    if (User.IsInRole("Patient"))
                    {
                        appointment.Diagnosis = originalAppointment.Diagnosis;
                        appointment.Medication = originalAppointment.Medication;
                        appointment.Treatment_Plan = originalAppointment.Treatment_Plan;
                        appointment.Common_tests = originalAppointment.Common_tests;
                        appointment.Notes = originalAppointment.Notes;
                        appointment.Status = originalAppointment.Status;
                        appointment.StaffId = originalAppointment.StaffId;
                        appointment.PatientId = originalAppointment.PatientId;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(appointment.Diagnosis))
                        {
                            ModelState.AddModelError("Diagnosis", "The Diagnosis field is required for Doctors/Staff.");

                            var pList = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

                            var sList = _context.Staff
                                .Where(s => s.Position == "Doctor")
                                .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                                .ToList();

                            ViewData["PatientId"] = new SelectList(pList, "PatientId", "FullName", appointment.PatientId);
                            ViewData["StaffId"] = new SelectList(sList, "StaffId", "FullName", appointment.StaffId);

                            return View(appointment);
                        }
                    }

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Appointment updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        var pList = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

                        var sList = _context.Staff
                            .Where(s => s.Position == "Doctor")
                            .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                            .ToList();

                        ViewData["PatientId"] = new SelectList(pList, "PatientId", "FullName", appointment.PatientId);
                        ViewData["StaffId"] = new SelectList(sList, "StaffId", "FullName", appointment.StaffId);
                        throw;
                    }
                }
            }

            var patientsFail = _context.Patient.Select(p => new { p.PatientId, FullName = p.FName + " " + p.LName }).ToList();

            var staffFail = _context.Staff
                .Where(s => s.Position == "Doctor")
                .Select(s => new { s.StaffId, FullName = s.Fname + " " + s.Lname })
                .ToList();

            ViewData["PatientId"] = new SelectList(patientsFail, "PatientId", "FullName", appointment.PatientId);
            ViewData["StaffId"] = new SelectList(staffFail, "StaffId", "FullName", appointment.StaffId);

            return View(appointment);
        }

        // GET: Appointments/Delete/5
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointment.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointment.Remove(appointment);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointment.Any(e => e.AppointmentId == id);
        }

        // AUTOCOMPLETE FOR PATIENT SEARCH
        [HttpGet]
        public async Task<IActionResult> SearchPatients(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            var patients = await _context.Patient
                .Where(p => p.FName.Contains(term) || p.LName.Contains(term))
                .Select(p => new { id = p.PatientId, name = p.FName + " " + p.LName })
                .Take(10)
                .ToListAsync();

            return Json(patients);
        }

        // PRINT REPORT
        public async Task<IActionResult> PrintReport(DateTime? fromDate, DateTime? toDate, int? staffId, string status)
        {
            var appointmentsQuery = _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Visit_Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Visit_Date <= toDate.Value);
            }

            if (staffId.HasValue)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.StaffId == staffId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.Status.ToString() == status);
            }

            var reportData = await appointmentsQuery.OrderByDescending(a => a.Visit_Date).ToListAsync();

            return View("PrintReport", reportData);
        }

        // PRINT SINGLE APPOINTMENT (For Patient & Doctor)
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .Include(a => a.Staff)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // CANCEL APPOINTMENT (Patient Only)
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointment
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null) return NotFound();

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

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.Notes += $"\n[Cancelled by patient on {DateTime.Now:yyyy-MM-dd HH:mm}]";

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء الموعد بنجاح.";
            return RedirectToAction("Index");
        }
    }
}