using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Patient
    {
        [Key]
        public string PatientId { get; set; }

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
         public string Gender { get; set; }

        // Relation Between Patient & Appointment (1 Patient many Appointment) 
        public ICollection<Appointment> Appointments { get; set; } // [One]
        // Relation Between Patient & Medical_Record  (1 Patient many Medical_Record)
        public ICollection<Medical_Record> Medical_Records { get; set; } //[One]

    }
}
