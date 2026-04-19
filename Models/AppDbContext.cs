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
            base.OnModelCreating(modelBuilder);


            // Relationship between Patient and Appointment (1 Patient To Many Appointment)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Appointment) // Patient has Many Appointments
                .WithOne(a => a.Patient) // Appointment has One Patients
                .HasForeignKey<Appointment>(a => a.PatientID); // Foreign Key in Appointment



            // Relationship between Staff and Appointment (1 Staff To Many Appointment)
            modelBuilder.Entity<Staff>()
                .HasMany(s => s.Appointments) // Patient has Many Appointments
                .WithOne(a => a.Staff) // Appointment has One Patients
                .HasForeignKey(a => a.StaffID); // Foreign Key in Appointment


            //Relationship Between STAFF and DEPARTMENT (  ONE    DEPARTMENT   TO  MANY  STAFF  )
            modelBuilder.Entity<Department>()
                .HasMany(d => d.Staffs)
                .WithOne(s => s.Department)
                .HasForeignKey(s => s.DeptID);


            // Relation Between  Pateint & Pateint_Medical_Profile  (1  Pateint One Patient_Medical_Profile)

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Patient_Medical_Profile)
                .WithOne(mp => mp.Patient)
                .HasForeignKey<Patient_Medical_Profile>(mp => mp.PatientID);
        }
    }
}