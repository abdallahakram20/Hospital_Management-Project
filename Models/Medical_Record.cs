using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Medical_Record
    {
        [Key]
        public string Medical_RecordId { get; set; }

        
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

        public string AppointmentID { get; set; }
        
        public virtual Appointment Appointment { get; set; }
        // relation between  Patient & Medical_Record (1 Patient Meny Medical_Record)

        public string PatientID { get; set; }
        public virtual Patient Patient { get; set; }


        //relation between Staff & Medical_Record (1 Staff Meny Medical_Record)

        public string StaffID { get; set; }
        public Staff Staff { get; set; }


    }
}