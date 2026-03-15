using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Patient
    {
        [Key]
        [MaxLength(20)]
        public string patientId { get; set; }
        [Required]
        [MaxLength(50)]
        public string FName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LName { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }
         public enum Gender
        {
            Male,
            Female
        }
    }
}
