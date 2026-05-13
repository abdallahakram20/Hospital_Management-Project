using Hospital_Management_Project.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // تأكد من إضافة هذا السطر

namespace Hospital_Management_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. إعداد قاعدة البيانات
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            );

            // 2. إضافة خدمة الكوكيز (مهم جداً للـ Login)
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // المسار الذي يتم تحويل المستخدم إليه لو حاول دخول صفحة محظورة
                    options.AccessDeniedPath = "/Account/AccessDenied"; // المسار لو حاول شخص دخول صفحة ليست من صلاحياته
                    options.ExpireTimeSpan = TimeSpan.FromDays(7); // تذكر الدخول لمدة 7 أيام
                });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // 3. ترتيب الميدل وير ضروري جداً (Authentication قبل Authorization)
            app.UseAuthentication();
            app.UseAuthorization();

            // 4. تعديل المسار الافتراضي لفتح صفحة اللوجن أولاً
            app.MapControllerRoute(
            name: "login",
            pattern: "login",
            defaults: new { controller = "Account", action = "Login" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();

                if (!context.Department.Any())
                {
                    context.Department.Add(new Department { DeptName = "Administration" });
                    context.SaveChanges();
                }

                var adminEmail = "admin@hospital.com"; // ضع إيميلك هنا
                var adminExists = context.Staff.Any(s => s.Email == adminEmail);

                if (!adminExists)
                {
                    var adminUser = new Staff
                    {
                        Fname = "Abdallah",
                        Lname = "Akram",
                        Email = adminEmail,
                        Position = "Admin", 
                        DepartmentId = context.Department.First().DepartmentId,
                        
                        Password = BCrypt.Net.BCrypt.HashPassword("Kali")
                    };

                    context.Staff.Add(adminUser);
                    context.SaveChanges();
                }
            }
            app.Run();
        }
    }
}