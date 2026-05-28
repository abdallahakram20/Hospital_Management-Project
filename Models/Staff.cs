using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {
        [Key]
        public int StaffId { get; set; }
        [MaxLength(50)]
        public string Position { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [MaxLength(10)]
        public string Fname { get; set; } = string.Empty;
        [MaxLength(10)]
        public string Lname { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [NotMapped]
        public string StaffName => $"{Fname} {Lname}";

        // Relation Between Department & Staff (1 Department to Many Staff) [Many]
        public int DepartmentId { get; set; }
        public virtual Department? Department { get; set; }
        // Relation Between Staff & Appointment (1 Staff many Appointment) 
        public ICollection<Appointment>? Appointments { get; set; }
    }
}