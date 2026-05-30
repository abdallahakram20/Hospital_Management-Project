using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class Department
    {

        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string? DeptName { get; set; }
        [Required]
        [MaxLength(50)]
        public string? DeptFloor { get; set; }

        // Relation Between Department & Staff (1 Department many Staff) 
        public ICollection<Staff>? Staffs { get; set; }
    }

}
