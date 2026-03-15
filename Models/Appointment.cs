using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Appointment
    {
        public string Appointment_ID { get; set; }

        [Required]
        public string Patient_ID { get; set; }

        [Required]
        public string Staff_ID { get; set; }

        [Required]
        public DateTime Appointment_Date { get; set; }
        public string Status { get; set; }  
        public string Reason { get; set; }
    }
}
