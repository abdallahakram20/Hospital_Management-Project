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
            ModelState.Remove("ImageFile"); // Crucial fix: prevents required validation block if no new image is provided

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

                // Redirects back to the Index page instead of showing a blank page
                return RedirectToAction(nameof(Index));
            }

            // If validation fails in traditional post, re-populate the dropdown and return to view
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
    }
}