using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Appointment
    {
        [Key]
        public string AppointmentId { get; set; }

        [Required, DataType(DataType.DateTime)]
        public DateTime Appointment_Date { get; set; }
        [StringLength(50)]
        public string? Status { get; set; } 
        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        public string Diagnosis { get; set; }
        public string Medication { get; set; }
        public string Treatment_Plan { get; set; }

        [MaxLength(200)]
        public string Notes { get; set; }
        [MaxLength(200)]
        public string Common_tests { get; set; }

        // Relation Between Patient & Appointment (1 Patient With Many Appointment) 

        public string PatientID { get; set; }

        // Relation Between Staff & Appointment (1 Staff to Many Appointment) 

        public string StaffID { get; set; }

    }
}
