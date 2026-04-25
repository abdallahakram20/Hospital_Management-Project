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

            [Required]
            [MaxLength(10)]
            public string Gender { get; set; }

            [Required]
            [MaxLength(50)]
            public string user_name { get; set; }

            [Required]
            [MaxLength(100)]
            public string Password { get; set; }


            // Relation Between  Pateint & Pateint_Medical_Profile  (1  Pateint One Pateint_Medical_Profile)
            public virtual Patient_Medical_Profile Patient_Medical_Profile { get; set; }

            // Relation Between  Pateint & Appointment  (1  Pateint Many Appointment)

            public ICollection<Appointment> Appointments { get; set; }


    }
}
