using Hospital_Management_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    // Helper class to store doctor shifts in a JSON file (does not affect the database)
    public class DoctorShiftData
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class StaffsController(AppDbContext context, IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        // =======================================================
        // Functions to handle reading and writing doctor shifts from/to the JSON file
        // =======================================================
        private string GetShiftsFilePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "doctor_shifts.json");
        }

        private Dictionary<int, DoctorShiftData> GetAllShifts()
        {
            var path = GetShiftsFilePath();
            if (!System.IO.File.Exists(path)) return new Dictionary<int, DoctorShiftData>();
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<int, DoctorShiftData>>(json) ?? new Dictionary<int, DoctorShiftData>();
        }

        private void SaveShiftForDoctor(int staffId, TimeSpan start, TimeSpan end)
        {
            var shifts = GetAllShifts();
            shifts[staffId] = new DoctorShiftData { StartTime = start, EndTime = end };
            System.IO.File.WriteAllText(GetShiftsFilePath(), JsonSerializer.Serialize(shifts));
        }

        private void DeleteShiftForDoctor(int staffId)
        {
            var shifts = GetAllShifts();
            if (shifts.Remove(staffId))
            {
                System.IO.File.WriteAllText(GetShiftsFilePath(), JsonSerializer.Serialize(shifts));
            }
        }
        // =======================================================

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

            var staff = await _context.Staff.Include(s => s.Department).Include(s => s.Appointments).FirstOrDefaultAsync(m => m.StaffId == id);
            if (staff == null) return NotFound();

            // Authorization Checks
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase) &&
                staff.Email != userEmail)
            {
                return Forbid();
            }

            // Retrieve shift details from the JSON file
            var shifts = GetAllShifts();
            if (shifts.TryGetValue(staff.StaffId, out var shift))
            {
                ViewBag.ShiftText = $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}";
            }
            else
            {
                ViewBag.ShiftText = "Not Defined (Default: 09:00 - 17:00)";
            }

            return View(staff);
        }

        // GET: Staffs/Create
        public IActionResult Create()
        {
            if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();
            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName");
            return View();
        }

        // POST: Staffs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,Position,Email,Password,Fname,Lname,DepartmentId,ImageFile,ImagePath")] Staff staff, TimeSpan shiftStartTime, TimeSpan shiftEndTime)
        {
            if (!string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)) return Forbid();

            if (ModelState.IsValid)
            {
                // Upload Image Logic
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

                // Save shift data to JSON if the created staff member is a Doctor
                if (staff.Position == "Doctor")
                {
                    SaveShiftForDoctor(staff.StaffId, shiftStartTime, shiftEndTime);
                }

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

            // Authorization Checks
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && staff.Email != currentUserEmail) return Forbid();

            // Get the shift from the JSON file to populate the form fields
            var shifts = GetAllShifts();
            if (shifts.TryGetValue(staff.StaffId, out var shift))
            {
                ViewBag.ShiftStartTime = shift.StartTime;
                ViewBag.ShiftEndTime = shift.EndTime;
            }
            else
            {
                ViewBag.ShiftStartTime = new TimeSpan(9, 0, 0); // Default 09:00 AM
                ViewBag.ShiftEndTime = new TimeSpan(17, 0, 0);  // Default 05:00 PM
            }

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,Email,Fname,Lname,DepartmentId,Position")] Staff staff, IFormFile ImageFile, TimeSpan shiftStartTime, TimeSpan shiftEndTime)
        {
            if (id != staff.StaffId) return NotFound();

            var existingStaff = await _context.Staff.FindAsync(id);
            if (existingStaff == null) return NotFound();

            // Authorization Checks
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.Equals(userRole, "Patient", StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) && existingStaff.Email != currentUserEmail) return Forbid();

            // Clear unchanged model properties from validation requirements
            ModelState.Remove("Password");
            ModelState.Remove("ImagePath");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                existingStaff.Fname = staff.Fname;
                existingStaff.Lname = staff.Lname;
                existingStaff.Email = staff.Email;
                existingStaff.DepartmentId = staff.DepartmentId;

                if (User.IsInRole("Admin") && !string.IsNullOrEmpty(staff.Position))
                {
                    existingStaff.Position = staff.Position;
                }

                // Image Upload Logic
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/staff");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    await ImageFile.CopyToAsync(fileStream);
                    existingStaff.ImagePath = uniqueFileName;
                }

                await _context.SaveChangesAsync();

                // Save the modified shift to the JSON file
                if (existingStaff.Position == "Doctor")
                {
                    SaveShiftForDoctor(existingStaff.StaffId, shiftStartTime, shiftEndTime);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
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
                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();

                // Remove doctor's shift from the JSON file
                DeleteShiftForDoctor(id);
            }
            return RedirectToAction(nameof(Index));
        }

        // =======================================================
        // GET: Staffs/ChangePassword/5
        // Updates to pass data via ViewBag instead of strongly-typed Model
        // =======================================================
        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Guard Clause: Only allow access if the authenticated user is a "Doctor" and owns this record
            if (!string.Equals(userRole, "Doctor", StringComparison.OrdinalIgnoreCase) || staff.Email != currentUserEmail)
            {
                return Forbid();
            }

            // Seed ViewBag parameters to support clean html form bindings
            ViewBag.StaffId = staff.StaffId;
            ViewBag.ErrorMessage = null;

            return View();
        }

        // =======================================================
        // POST: Staffs/ChangePassword/5
        // Processes individual string form elements without requiring a strict Model mapping
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int staffId, string currentPassword, string newPassword, string confirmPassword)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Secondary Guard Check: Prevent cross-account submissions via raw form exploits
            if (!string.Equals(userRole, "Doctor", StringComparison.OrdinalIgnoreCase) || staff.Email != currentUserEmail)
            {
                return Forbid();
            }

            // Server-side validation check for empty inputs
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.StaffId = staffId;
                ViewBag.ErrorMessage = "All password fields are required.";
                return View();
            }

            // Security Check: Verify if current password fits database hash record
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, staff.Password))
            {
                ViewBag.StaffId = staffId;
                ViewBag.ErrorMessage = "The current password you entered is incorrect.";
                return View();
            }

            // Match confirmation password check
            if (newPassword != confirmPassword)
            {
                ViewBag.StaffId = staffId;
                ViewBag.ErrorMessage = "The new password and confirmation password do not match.";
                return View();
            }

            // Securely hash the password using BCrypt algorithm to maintain schema harmony
            staff.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            _context.Update(staff);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}