using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Appointment
    {
        [key]
        public string Appointment_ID { get; set; }

        [Required]
        public string Patient_ID { get; set; }

        [Required]
        public string Staff_ID { get; set; }

        [Required]
        public DateTime Appointment_Date { get; set; }
        public string Status { get; set; }  
        public string Reason { get; set; }

        // Relation Between Patient & Appointment (1 Patient to Many Appointment) [Many]
        public string DeptID { get; set; }

        // Relation Between Staff & Appointment (1 Staff to Many Appointment) [Many]
        public string StaffID { get; set; }


    }
}
