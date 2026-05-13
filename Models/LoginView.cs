using System.ComponentModel.DataAnnotations;

namespace Hospital_Management_Project.Models
{
    public class LoginView
    {
        // Indicates which tab/form was used: "Staff" or "Patient"
        public string UserType { get; set; }

        // Staff email (optional for Patient login; validate in controller as needed)
        [EmailAddress]
        public string Email { get; set; }

        // Patient username (optional for Staff login; validate in controller as needed)
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}