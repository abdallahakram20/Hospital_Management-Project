using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {

        [Key]
        public string StaffId { get; set; }

        [MaxLength(50)]
        public string Position { get; set; }

        [Required,EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }

        [MaxLength(10)]
        public string Fname { get; set; }

        [MaxLength(10)]
        public string Lname { get; set; }

        // Relation Between Department & Staff (1 Department to Many Staff) [Many]
        public string DeptId { get; set; }

        // Relation Between Staff & Appointment (1 Staff many Appointment) 
        public ICollection<Appointment> Appointments { get; set; } 

    }
}
