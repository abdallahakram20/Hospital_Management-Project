using Hospital_Management_Project.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Hospital_Management_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Database Context Configuration
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // 2. Cookie Authentication Configuration
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";

                    // Set cookie expiration time on idle (e.g., 20 minutes)
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);

                    // Enable sliding expiration to renew cookie if user is active
                    options.SlidingExpiration = true;
                });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Routing Layouts
            app.MapControllerRoute(
                name: "login",
                pattern: "login",
                defaults: new { controller = "Account", action = "Login" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // --- Database Automated Data Seeding & Migrations ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();

                    // تطبيق المايجريشنز تلقائياً بدون مسح البيانات مسبقاً
                    context.Database.Migrate();

                    // استدعاء دالة بناء البيانات الأساسية (القسم والآدمين فقط)
                    SeedDatabase(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // أمر تشغيل التطبيق (نسخة واحدة فقط لمنع الأخطاء)
            app.Run();

            // دالة الـ Seeding النظيفة (أقسام أساسية + حساب الآدمين فقط)
            void SeedDatabase(AppDbContext context)
            {
                // 1. فحص وإضافة قسم الإدارة الأساسي إذا لم يكن موجوداً
                var adminDept = context.Department.FirstOrDefault(d => d.DeptName == "Administration");
                if (adminDept == null)
                {
                    adminDept = new Department
                    {
                        DeptName = "Administration",
                        DeptFloor = "Ground Floor"
                    };
                    context.Department.Add(adminDept);
                    context.SaveChanges();
                }

                // 2. فحص وإضافة حساب الآدمين الرئيسي فقط (البريد: admin@hospital.com والباسورد: admin123)
                if (!context.Staff.Any(s => s.Email == "admin@hospital.com"))
                {
                    context.Staff.Add(new Staff
                    {
                        Fname = "Admin",
                        Lname = "User",
                        Position = "Admin",
                        Email = "admin@hospital.com",
                        Password = BCrypt.Net.BCrypt.HashPassword("admin123"), // الباسورد الافتراضي للدخول
                        DepartmentId = adminDept.DepartmentId
                    });
                    context.SaveChanges();
                }

                // تم حذف (الدكاترة، موظفي الاستقبال، المرضى، والمواعيد التلقائية) بالكامل بناءً على طلبك.
                Console.WriteLine("✅ Database initialized successfully with Admin account only!");
            }
        }
    }
}