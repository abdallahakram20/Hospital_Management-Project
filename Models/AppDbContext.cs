using Microsoft.EntityFrameworkCore;

namespace Hospital_Management_Project.Models
{
    public class AppDbContext : DbContext
    {
        // Name Of My Data Base
        public AppDbContext(DbContextOptions <AppDbContext> options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }
    }
}
