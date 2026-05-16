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
    [Authorize] // Secure the entire controller - authentication is required for all actions
    public class StaffsController : Controller
    {
        private readonly AppDbContext _context;

        public StaffsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Staffs
        // Admin and Patients can view the list (filtered conditionally in the View). Regular staff members can only view themselves.
        public IActionResult Index(string searchString)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // 1. Fetch data and completely exclude Admins from the general staff directory listing
            IQueryable<Staff> staffList = _context.Staff
                                                 .Include(s => s.Department)
                                                 .Include(s => s.Appointments)
                                                 .Where(s => s.Position != "Admin");

            // 2. Regular staff members (not Admin or Patient) can exclusively view their own individual record
            if (userRole != "Admin" && userRole != "Patient")
            {
                staffList = staffList.Where(s => s.Email == userEmail);
            }

            // 3. Apply search filter conditions for Admin users only
            if (!string.IsNullOrEmpty(searchString) && userRole == "Admin")
            {
                staffList = staffList.Where(s => s.Fname.Contains(searchString)
                                              || s.Lname.Contains(searchString)
                                              || s.StaffId.ToString() == searchString);
            }

            return View(staffList.ToList());
        }

        // GET: Staffs/Details

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Security: Patients are completely blocked from viewing internal management staff profiles
            if (userRole == "Patient") return Forbid();

            // Security: Regular staff members cannot spy on other staff details
            if (userRole != "Admin" && staff.Email != userEmail) return Forbid();

            return View(staff);
        }

        // GET: Staffs/Create
        // Restricted to Admin users only (Patients and standard Staff are barred)
        public IActionResult Create()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin") return Forbid();

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName");
            return View();
        }

        // POST: Staffs/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,Position,Email,Password,Fname,Lname,DepartmentId")] Staff staff)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();

            if (ModelState.IsValid)
            {
                staff.Password = BCrypt.Net.BCrypt.HashPassword(staff.Password);
                _context.Add(staff);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // GET: Staffs/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Patients are barred from modifying staff records
            if (userRole == "Patient") return Forbid();
            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // POST: Staffs/Edit

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,Email,Fname,Lname,DepartmentId")] Staff staff)
        {
            if (id != staff.StaffId) return NotFound();

            var existingStaff = await _context.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.StaffId == id);
            if (existingStaff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient") return Forbid();
            if (userRole != "Admin" && existingStaff.Email != currentUserEmail) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    // Protection: Prevent role parameter tempering if a non-admin submits updates

                    if (userRole != "Admin")
                    {
                        staff.Position = existingStaff.Position;
                        staff.Password = existingStaff.Password;
                    }
                    else
                    {
                        staff.Password = existingStaff.Password;
                    }

                    _context.Update(staff);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StaffExists(staff.StaffId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // GET: Staffs/ChangePassword

        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient") return Forbid();
            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

            return View(staff);
        }

        // POST: Staffs/ChangePassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole == "Patient") return Forbid();
            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

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
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();

            if (id == null) return NotFound();

            var staff = await _context.Staff
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            return View(staff);
        }

        // POST: Staffs/Delete

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();

            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
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
        // Accessible exclusively by Admin to monitor staff list and change user permissions/roles directly

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
        // Server endpoint designed to update user positions and claim parameters securely

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int staffId, string newPosition)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            // Guard Clause: Prevent arbitrary API calls from tampering with Administrator privileges

            if (staff.Position == "Admin")
            {
                TempData["Error"] = "Modifying primary Administrator account settings is strictly prohibited!";
                return RedirectToAction(nameof(ManageRoles));
            }

            if (!string.IsNullOrEmpty(newPosition))
            {
                staff.Position = newPosition;
                _context.Update(staff);
                await _context.SaveChangesAsync();

                // Fixed: Explicitly concatenated Fname and Lname fields for english clean output

                TempData["Success"] = $"Staff member {staff.Fname} {staff.Lname} has been successfully assigned to the role of: {newPosition}.";
            }

            return RedirectToAction(nameof(ManageRoles));
        }
    }
}