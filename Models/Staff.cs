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

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;  // ✅

        // ✅ FIX: تغيير MaxLength من 10 إلى 50
        // 10 حروف فقط قليلة جداً، والآن بـ 50 يمكن تخزين أسماء طويلة
        [MaxLength(50)]
        public string Fname { get; set; } = string.Empty;

        // ✅ FIX: تغيير MaxLength من 10 إلى 50
        [MaxLength(50)]
        public string Lname { get; set; } = string.Empty;

        [MaxLength(255)]  // ✅ حد معقول
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