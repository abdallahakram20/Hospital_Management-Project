using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Management_Project.Models
{
    public class Patient_Medical_Profile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        [MaxLength(5)]
        [Display(Name = "Blood Type")]
        public string Blood_Type { get; set; }

        [Display(Name = "High Blood Pressure")]
        public bool Blood_Pressure { get; set; }

        [MaxLength(500)]
        [Display(Name = "Chronic Disease")]
        public string Chronic_Disease { get; set; }

        [MaxLength(500)]
        public string Allergies { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Weight { get; set; }

        [Display(Name = "Diabetes")]
        public bool Diabets { get; set; }

        [Required]
        public int PatientId { get; set; }

        public virtual Patient? Patient { get; set; }
    }
}