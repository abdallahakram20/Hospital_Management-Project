using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Medical_Record
    {
        [Key]
        [MaxLength(20)]
        public string Med_Rec_ID { get; set; }

        [Required]
        [MaxLength(20)]
        public string Appointment_id { get; set; }

        [Required]
        public DateTime Visit_Date { get; set; }

        [Required]
        public decimal Bills { get; set; }

        [Required]
        public string Diagnosis { get; set; }
        public string Medication { get; set; }
        public string Treatment_Plan { get; set; }




        //                        Relation

        // relation between  Appointment & Medical_Record (1 Appointment Meny Medical_Record)
        [Required]
        [MaxLength(20)]
        public string Appointment_id { get; set; }
        public Appointment Appointment { get; set; }

        // relation between  Patient & Medical_Record (1 Patient Meny Medical_Record)

        [Required]
        [MaxLength(20)]
        public string Patient_id { get; set; }
        public Patient Patient { get; set; }

        //relation between Staff & Medical_Record (1 Staff Meny Medical_Record)

        [Required]
        [MaxLength(20)]
        public string Staff_id { get; set; }
        public Staff Staff { get; set; }


    }
}