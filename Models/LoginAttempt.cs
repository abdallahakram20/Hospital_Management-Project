using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class LoginAttempt
    {
        [Key]
        public int AttemptId { get; set; }
        public string Email { get; set; }
        public DateTime AttemptTime { get; set; }
        public bool Success { get; set; }
    }
}
