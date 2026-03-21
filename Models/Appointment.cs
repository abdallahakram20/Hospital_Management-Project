using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Appointment
    {
        [Key,MaxLength(20)]
        public string Appointment_ID { get; set; }

        [Required,DataType(DataType.DateTime)]
        public DateTime Appointment_Date { get; set; }
        [StringLength(50)]
        public string? Status { get; set; } ="Scheduled";
        [StringLength(500)]
        public string? Reason { get; set; }

        // Relation Between Patient & Appointment (1 Patient to Many Appointment) [Many]
        [Required]
        public string PatientID { get; set; }
        public Patient Patient { get; set; }

        // Relation Between Staff & Appointment (1 Staff to Many Appointment) [Many]
        [Required]
        public string StaffID { get; set; }
        public Staff Staff { get; set; }


    }
}
