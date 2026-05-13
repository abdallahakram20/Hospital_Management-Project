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
    [Authorize] // تأمين الكنترولر بالكامل - يجب تسجيل الدخول للوصول
    public class StaffsController : Controller
    {
        private readonly AppDbContext _context;

        public StaffsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Staffs
        // الأدمن يرى الجميع، الموظف يرى نفسه فقط
        public IActionResult Index(string searchString)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            IQueryable<Staff> staffList = _context.Staff.Include(s => s.Department);

            // إذا لم يكن أدمن، يتم فلترة القائمة ليرى حسابه فقط
            if (userRole != "Admin")
            {
                staffList = staffList.Where(s => s.Email == userEmail);
            }

            if (!string.IsNullOrEmpty(searchString) && userRole == "Admin")
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

            var staff = await _context.Staff
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.StaffId == id);

            if (staff == null) return NotFound();

            // التحقق من الصلاحية: أدمن أو صاحب الحساب
            var userEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && staff.Email != userEmail) return Forbid();

            return View(staff);
        }

        // GET: Staffs/Create
        // متاح للأدمن فقط
        public IActionResult Create()
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();

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
                // تشفير الباسورد قبل الحفظ في قاعدة البيانات
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

            // التحقق: الموظف يعدل نفسه فقط أو الأدمن يعدل أي شخص
            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

            ViewData["DepartmentId"] = new SelectList(_context.Department, "DepartmentId", "DeptName", staff.DepartmentId);
            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,Email,Fname,Lname,DepartmentId")] Staff staff)
        {
            if (id != staff.StaffId) return NotFound();

            // جلب البيانات الأصلية بدون تتبع (AsNoTracking) للمقارنة والحماية
            var existingStaff = await _context.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.StaffId == id);
            if (existingStaff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // منع التعديل إذا لم يكن أدمن أو صاحب الحساب
            if (userRole != "Admin" && existingStaff.Email != currentUserEmail) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    // حماية: إذا لم يكن أدمن، يتم استرجاع المنصب والباسورد الأصليين لمنع التلاعب
                    if (userRole != "Admin")
                    {
                        staff.Position = existingStaff.Position;
                        staff.Password = existingStaff.Password;
                    }
                    else
                    {
                        // للأدمن: نحافظ على الباسورد القديم (لأن التعديل هنا لا يشمل الباسورد)
                        staff.Password = existingStaff.Password;
                        // يمكن للأدمن تعديل المنصب إذا أردت إضافة الحقل في الـ View
                        // staff.Position = ...
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

        // GET: Staffs/ChangePassword/5 (إعادة تعيين الباسورد)
        public async Task<IActionResult> ChangePassword(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff.FindAsync(id);
            if (staff == null) return NotFound();

            var currentUserEmail = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // الأدمن يغير للكل، الموظف يغير لنفسه فقط
            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

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
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && staff.Email != currentUserEmail) return Forbid();

            if (!string.IsNullOrEmpty(newPassword))
            {
                // تشفير الباسورد الجديد باستخدام BCrypt
                staff.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.Update(staff);
                await _context.SaveChangesAsync();

                // إذا غير الموظف باسورده لنفسه، قد ترغب في توجيهه للهوم
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "كلمة المرور لا يمكن أن تكون فارغة.");
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

        // POST: Staffs/Delete/5
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
    }
}