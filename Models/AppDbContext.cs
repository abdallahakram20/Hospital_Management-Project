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
        public DbSet<Medical_Record> Medical_Records { get; set; }
        public DbSet<Patient> Patient { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<Department> Department { get; set; }
        public DbSet<Patient_Medical_Profile> PatientMedicalProfiles { get; set; }



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



            //Relationship between Appointment and Medical_Record ( 1 Appointment TO Many Medical_Record)
            modelBuilder.Entity<Appointment>()
                .HasMany(a => a.Medical_Records)
                .WithOne(m => m.Appointment)
                .HasForeignKey(m => m.AppointmentID);

            //Relationship between Staff and Medical_Record ( 1 Staff TO Many Medical_Record)
            modelBuilder.Entity<Staff>()
               .HasMany(s => s.Medical_Records)
               .WithOne(m => m.Staff)
               .HasForeignKey(m => m.StaffID)
               .OnDelete(DeleteBehavior.Restrict);


            //Relationship between Patient and Medical_Record ( 1 Patient TO Many Medical_Record)
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Medical_Records)
                .WithOne(m => m.Patient)
                .HasForeignKey(m => m.PatientID)
                .OnDelete(DeleteBehavior.Restrict);


            //Relationship Between STAFF and DEPARTMENT (  ONE    DEPARTMENT   TO  MANY  STAFF  )
            modelBuilder.Entity<Department>()
                .HasMany(d => d.Staff)
                .WithOne(s => s.Department)
                .HasForeignKey(s => s.DeptID);

            //Relationship Between Pateint_Medical_Profile and Medical_Record (  ONE    Pateint_Medical_Profile   TO  MANY  Medical_Record  )
            modelBuilder.Entity<Patient_Medical_Profile>()
                .HasMany(pmp => pmp.Medical_Records)
                .WithOne(m => m.Patient_Medical_Profile)
                .HasForeignKey(m => m.Patient_ProfileID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation Between  Pateint & Pateint_Medical_Profile  (1  Pateint One Pateint_Medical_Profile)

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Patient_Medical_Profile)
                .WithOne(mp => mp.Patient)
                .HasForeignKey<Patient_Medical_Profile>(mp => mp.PatientID);
        }
    }
}