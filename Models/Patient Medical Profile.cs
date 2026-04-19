using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Management_Project.Models
{
    public class Patient_Medical_Profile
    {
        [Key]
        public string ProfileId { get; set; }
        [Required]
        [MaxLength(5)]
        public string Blood_Type { get; set; }
        public bool Blood_Pressure { get; set; }
        [MaxLength(500)]
        public string Chronic_Disease { get; set; }
        [MaxLength(500)]
        public string Allergies { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Weight { get; set; }
        public bool Diabets { get; set; }

        // Relation Between  Pateint_Medical_Profile & Medical_Record  (1  Pateint_Medical_Profile many Medical_Record)
        public ICollection<Medical_Record> Medical_Records { get; set; }


        // Relation Between  Pateint & Pateint_Medical_Profile  (1  Pateint One Pateint_Medical_Profile)
        public string PatientID { get; set; }
        public virtual Patient Patient { get; set; } 



    }
}
