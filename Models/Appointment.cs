using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTime Visit_Date { get; set; }

        // في Models/AppointmentStatus.cs
        
        // في Appointment.cs
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Available;

        [StringLength(500)]
        public string? Reason { get; set; }

        // ✅ FIX: اشلنا [Required] لأن Diagnosis هي nullable (string?)
        // الآن بـ Optional تماماً مثل الحقول الأخرى
        public string? Diagnosis { get; set; }

        public string? Medication { get; set; }

        public string? Treatment_Plan { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        [MaxLength(200)]
        public string? Common_tests { get; set; }

        // Relation Between Patient & Appointment (1 Patient With Many Appointment) 
        public int PatientId { get; set; }
        public virtual Patient? Patient { get; set; }

        // Relation Between Staff & Appointment (1 Staff to Many Appointment) 
        public int StaffId { get; set; }
        public virtual Staff? Staff { get; set; }
    }
}