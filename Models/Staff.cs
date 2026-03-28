using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {

        [Key]
        public string Staff_ID { get; set; }
        public string Dept_ID { get; set; }
        public string Position { get; set; }
        public string Fname { get; set; }
        public string Lname { get; set; }

        public Department Department { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<Medical_Record> Medical_Records { get; set; }



        //                  Relation


        // Relation Between Staff & Appointment (1 Staff many Appointment) 
        public ICollection<Appointment> Appointments { get; set; } //[One]

        // Relation Between Staff & Medical_Record  (1 Staff many Medical_Record) 
        public ICollection<Medical_Record> Medical_Records { get; set; } //[one]


    }
}
