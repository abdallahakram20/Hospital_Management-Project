using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Management_Project.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string user_name { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;  // ✅
                                                              // في Patient.cs
        public bool IsDeleted { get; set; } = false;  // ✅ Soft Delete
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        [NotMapped]
        public string PatientName => $"{FName} {LName}";

        // Relation Between Patient & Patient_Medical_Profile (1 Patient One Patient_Medical_Profile)
        public virtual Patient_Medical_Profile? Patient_Medical_Profile { get; set; }

        // Relation Between Patient & Appointment (1 Patient Many Appointment)
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}