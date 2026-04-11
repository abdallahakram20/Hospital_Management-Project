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
        public string? Status { get; set; } = "Scheduled";
        [StringLength(500)]
        public string? Reason { get; set; }

        // Relation Between Patient & Appointment (1 Patient to Many Appointment) [Many]

        public string PatientID { get; set; }
        public virtual Patient Patient { get; set; }

        // Relation Between Staff & Appointment (1 Staff to Many Appointment) [Many]

        public string StaffID { get; set; }
        public virtual Staff Staff { get; set; }


        // Relation Between Medical_Record & Appointment (1 Appointment to Many Medical_Record ) [one]
        public ICollection<Medical_Record> Medical_Records { get; set; }

    }
}
