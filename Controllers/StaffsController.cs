using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital_Management_Project.Controllers
{
    [Authorize] // Secure the entire Controller - requires login for all operations
    public class StaffsController(AppDbContext context, IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        // GET: Staffs
        public IActionResult Index(string searchString)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            IQueryable<Staff> staffList = _context.Staff
                                                 .Include(s => s.Department)
                                                 .Include(s => s.Appointments)
                                                 .Where(s => s.Position != "Admin");

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                staffList = staffList.Where(s => s.Email == userEmail);
            }

            if (!string.IsNullOrEmpty(searchString) && string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                staffList = staffList.Where(s => s.Fname.Contains(searchString)
                                              || s.Lname.Contains(searchString)
                                              || s.StaffId.ToString() == searchString);
            }

            return View(staffList.ToList());
        }

        // GET: Staffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Added .Include(s => s.Appointments) to ensure loading the doctor's appointments on the details page
            var staff = await _context.Staff
                .Include(s => s.Department)
                .Include(s => s.Appointments)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // 1. If the current user is "Patient" -> pass the doctor's data and display the page immediately without blocking
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase))
            {
                return View(staff);
            }

            // 2. Protection for other staff: If not Admin and does not own this personal account, deny access
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && staff.Email != userEmail)
            {
                return Forbid();
            }

            return View(staff);
        }

        // GET: Staffs/Create
        public IActionResult Create()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName");
            return View();
        }

        // POST: Staffs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,Position,Email,Password,Fname,Lname,DepartmentId,ImageFile,ImagePath")] Staff staff)
        {
            if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();

            if (ModelState.IsValid)
            {
                if (staff.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/staff");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + staff.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await staff.ImageFile.CopyToAsync(fileStream);
                    staff.ImagePath = uniqueFileName;
                }

                staff.Password = BCrypt.Net.BCrypt.HashPassword(staff.Password);
                _context.Add(staff);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // GET: Staffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && staff.Email != currentUserEmail) return Forbid();

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,Email,Fname,Lname,DepartmentId,Position")] Staff staff, IFormFile ImageFile)
        {
            if (id != staff.StaffId) return NotFound();

            var existingStaff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == id);
            if (existingStaff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && existingStaff.Email != currentUserEmail) return Forbid();

            ModelState.Remove("Password");
            ModelState.Remove("ImagePath");

            if (ModelState.IsValid)
            {
                try
                {
                    existingStaff.Fname = staff.Fname;
                    existingStaff.Lname = staff.Lname;
                    existingStaff.Email = staff.Email;
                    existingStaff.DepartmentId = staff.DepartmentId;

                    if (string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(staff.Position))
                    {
                        existingStaff.Position = staff.Position;
                    }

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/staff");
                        Directory.CreateDirectory(uploadsFolder);

                        if (!string.IsNullOrEmpty(existingStaff.ImagePath))
                        {
                            string oldFilePath = Path.Combine(uploadsFolder, existingStaff.ImagePath);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using var fileStream = new FileStream(filePath, FileMode.Create);
                        await ImageFile.CopyToAsync(fileStream);
                        existingStaff.ImagePath = uniqueFileName;
                    }

                    await _context.SaveChangesAsync();
                    return Ok();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StaffExists(staff.StaffId)) return NotFound();
                    else throw;
                }
            }

            return BadRequest(ModelState);
        }

        // GET: Staffs/ChangePassword/5
        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (staff.Email != currentUserEmail) return Forbid();

            return View(staff);
        }

        // POST: Staffs/ChangePassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (staff.Email != currentUserEmail) return Forbid();

            if (!string.IsNullOrEmpty(newPassword))
            {
                staff.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.Update(staff);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Password field cannot be left empty.");
            return View(staff);
        }

        // GET: Staffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (id == null) return NotFound();

            var staff = await _context.Staff
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            return View(staff);
        }

        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();

            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
                if (!string.IsNullOrEmpty(staff.ImagePath))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/staff", staff.ImagePath);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Staff.Remove(staff);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StaffExists(int id)
        {
            return _context.Staff.Any(e => e.StaffId == id);
        }

        // GET: Staffs/ManageRoles
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRoles()
        {
            var staffList = await _context.Staff
                                          .Include(s => s.Department)
                                          .Where(s => s.Position != "Admin")
                                          .ToListAsync();

            return View(staffList);
        }

        // POST: Staffs/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int staffId, string newPosition)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            if (string.Equals(staff.Position, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Modifying primary Administrator account settings is strictly prohibited!";
                return RedirectToAction(nameof(ManageRoles));
            }

            if (!string.IsNullOrEmpty(newPosition))
            {
                staff.Position = newPosition;
                _context.Update(staff);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Staff member {staff.Fname} {staff.Lname} has been successfully assigned to the role of: {newPosition}.";
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // ==========================================
        // Managing booking, modification, and cancellation of appointments from the patient side 
        // (Fully corrected without null and with 12-hour condition)
        // ==========================================

        // GET: Staffs/BookAppointment/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> BookAppointment(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Set<Appointment>().FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.PatientId > 0)
            {
                TempData["Error"] = "This appointment is already booked by another patient.";
                return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
            }

            return View(appointment);
        }

        // POST: Staffs/ConfirmBookAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ConfirmBookAppointment(int id)
        {
            var appointment = await _context.Set<Appointment>().FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.PatientId > 0)
            {
                TempData["Error"] = "Action Denied! This slot has just been reserved.";
                return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
            }

            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                appointment.PatientId = !string.IsNullOrEmpty(userIdClaim) ? int.Parse(userIdClaim) : 1;
            }
            catch
            {
                appointment.PatientId = 1;
            }

            _context.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment has been successfully booked!";
            return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
        }

        // GET: Staffs/CancelAppointment/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> CancelAppointment(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Set<Appointment>().FindAsync(id);
            if (appointment == null) return NotFound();

            DateTime appointmentDateTime = appointment.Visit_Date;
            double hoursLeft = (appointmentDateTime - DateTime.Now).TotalHours;

            if (hoursLeft <= 12)
            {
                TempData["Error"] = "Action denied! It is prohibited to cancel appointments with less than 12 hours remaining.";
                return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
            }

            appointment.PatientId = 0; // Reset to 0 to make it available again instead of using null
            _context.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your appointment has been cancelled successfully.";
            return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
        }

        // GET: Staffs/ChangeAppointment/5
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ChangeAppointment(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Set<Appointment>().FindAsync(id);
            if (appointment == null) return NotFound();

            DateTime appointmentDateTime = appointment.Visit_Date;
            if ((appointmentDateTime - DateTime.Now).TotalHours <= 12)
            {
                TempData["Error"] = "Action denied! It is prohibited to change appointments with less than 12 hours remaining.";
                return RedirectToAction(nameof(Details), new { id = appointment.StaffId });
            }

            ViewData["AlternativeAppointments"] = await _context.Set<Appointment>()
                .Where(a => a.StaffId == appointment.StaffId && a.PatientId == 0)
                .ToListAsync();

            return View(appointment);
        }

        // POST: Staffs/ConfirmChangeAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ConfirmChangeAppointment(int currentAppointmentId, int newAppointmentId)
        {
            var currentAppointment = await _context.Set<Appointment>().FindAsync(currentAppointmentId);
            var newAppointment = await _context.Set<Appointment>().FindAsync(newAppointmentId);

            if (currentAppointment == null || newAppointment == null) return NotFound();

            DateTime currentDateTime = currentAppointment.Visit_Date;
            if ((currentDateTime - DateTime.Now).TotalHours <= 12)
            {
                TempData["Error"] = "Action denied! Time lock is active (Less than 12 hours remaining).";
                return RedirectToAction(nameof(Details), new { id = currentAppointment.StaffId });
            }

            if (newAppointment.PatientId > 0)
            {
                TempData["Error"] = "The selected new appointment slot has just been taken by another user.";
                return RedirectToAction(nameof(Details), new { id = currentAppointment.StaffId });
            }

            newAppointment.PatientId = currentAppointment.PatientId;
            currentAppointment.PatientId = 0;

            _context.Update(currentAppointment);
            _context.Update(newAppointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your appointment has been successfully rescheduled to the new slot!";
            return RedirectToAction(nameof(Details), new { id = newAppointment.StaffId });
        }
    }
}