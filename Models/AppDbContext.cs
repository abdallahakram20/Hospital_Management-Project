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


        // Relation Beween Patient and Patient_Medical_Profile (One-to-One)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Patient_Medical_Profile) 
                .WithOne(mp => mp.Patient)             
                .HasForeignKey<Patient_Medical_Profile>(mp => mp.PatientId);
        }
    }
}
