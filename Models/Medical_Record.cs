using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital_Management_Project.Models
{
    public class Medical_Record
    {
        [Key]
        public string Medical_RecordId { get; set; }


        [Required]
        public DateTime Visit_Date { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 4)")]
        public decimal Bills { get; set; }

        [Required]
        public string Diagnosis { get; set; }
        public string Medication { get; set; }
        public string Treatment_Plan { get; set; }




        //                        Relations

        // relation between  Appointment & Medical_Record (1 Appointment Meny Medical_Record)

        public string AppointmentID { get; set; }

        public virtual Appointment Appointment { get; set; }
        // relation between  Patient & Medical_Record (1 Patient Meny Medical_Record)

        public string? PatientID { get; set; }
        public virtual Patient Patient { get; set; }


        //relation between Staff & Medical_Record (1 Staff Meny Medical_Record)

        // HEAD
        public string StaffID { get; set; }
        public virtual Staff Staff { get; set; }

        //Relation between Pateint_Medical_Profile & Medical_Record (1 Pateint_Medical_Profile Meny Medical_Record)

        public string Patient_ProfileID { get; set; }
        public virtual Patient_Medical_Profile Patient_Medical_Profile { get; set; }

    }
}