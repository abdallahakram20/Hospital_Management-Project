using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Hospital_Management_Project.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected AppDbContext()
        {
        }

        public DbSet<Appointment> Appointment { get; set; }
        public DbSet<Patient> Patient { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<Patient_Medical_Profile> PatientMedicalProfile { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        // في AppDbContext.cs
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // حذف تلقائي للمواعيد عند حذف المريض
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);  // ✅ حذف أوتوماتيكي

            // حذف تلقائي للمواعيد عند حذف الدكتور
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Staff)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.StaffId)
                .OnDelete(DeleteBehavior.Cascade);  // ✅ حذف أوتوماتيكي
        }

    }
}
