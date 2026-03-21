using Microsoft.EntityFrameworkCore;

namespace Hospital_Management_Project.Models
{
    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions <AppDbContext> options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }

        public DbSet<Appointment> Appointment { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationship between Patient and Appointment (1 Patient To Many Appointment)
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Appointments) // Patient has Many Appointments
                .WithOne(a => a.Patient) // Appointment has One Patients
                .HasForeignKey(a => a.PatientID); // Foreign Key in Appointment



            // Relationship between Staff and Appointment (1 Staff To Many Appointment)
            modelBuilder.Entity<Staff>()
                .HasMany(s => s.Appointments) // Patient has Many Appointments
                .WithOne(a => a.Staff) // Appointment has One Patients
                .HasForeignKey(a => a.StaffID); // Foreign Key in Appointment

        }
    }
}
