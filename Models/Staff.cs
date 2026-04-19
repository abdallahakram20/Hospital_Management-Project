using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Staff
    {

        [Key]
        public string StaffID { get; set; }
        public string Position { get; set; }
        public string Fname { get; set; }
        public string Lname { get; set; }

        // Relation Between Department & Staff (1 Department to Many Staff) [Many]
        public string DeptID { get; set; }
        public virtual Department Department { get; set; }

        // Relation Between Staff & Appointment (1 Staff many Appointment) 
        public ICollection<Appointment> Appointments { get; set; } //[One]

    }
}
