using Microsoft.EntityFrameworkCore;

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



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Patient_Medical_Profile) // المريض له بروفايل واحد
                .WithOne(mp => mp.Patient)             // والبروفايل له مريض واحد
                .HasForeignKey<Patient_Medical_Profile>(mp => mp.PatientId); // تحديد البروفايل كطرف تابع
        }
    }
}
