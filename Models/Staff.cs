using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {


        // Relation Between Staff & Appointment (1 Staff many Appointment) 
        public ICollection<Appointment> Appointments { get; set; } //[One]

        // Relation Between Staff & Medical_Record  (1 Staff many Medical_Record) 
        public ICollection<Medical_Record> Medical_Records { get; set; } //[one]


    }
}
