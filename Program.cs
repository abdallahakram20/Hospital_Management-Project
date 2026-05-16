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

            app.MapControllerRoute(
                name: "login",
                pattern: "login",
                defaults: new { controller = "Account", action = "Login" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // --- Database Automated Data Seeding ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();

                    // Ensure database and schema exist before seeding
                    context.Database.EnsureCreated();

                    // Seed Department table if empty
                    if (!context.Department.Any())
                    {
                        context.Department.Add(new Department { DeptName = "Administration" });
                        context.SaveChanges();
                    }

                    var adminEmail = "admin@hospital.com";
                    var adminExists = context.Staff.Any(s => s.Email == adminEmail);

                    if (!adminExists)
                    {
                        var adminDept = context.Department.FirstOrDefault(d => d.DeptName == "Administration")
                                         ?? context.Department.First();

                        var adminUser = new Staff
                        {
                            Fname = "Abdallah",
                            Lname = "Akram",
                            Email = adminEmail,
                            Position = "Admin",
                            DepartmentId = adminDept.DepartmentId,
                            Password = BCrypt.Net.BCrypt.HashPassword("kali")
                        };

                        context.Staff.Add(adminUser);
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.Run();
        }
    }
}