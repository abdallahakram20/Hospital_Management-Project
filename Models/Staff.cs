using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {
        [key]
        public string Staff_ID { get; set; }

        [Required]
        public string Dept_ID { get; set; }

        [Required,MaxLength(50)]
        public string FName { get; set; }
        [Required,MaxLength(50)]
        public string LName { get; set; }

        public enum Staff_Position{
            Male,
            Female
    }

        // Relation Between Staff & Appointment (1 Staff many Appointment) [One]
        public ICollection<Appointment> Appointments { get; set; }
    }
}
