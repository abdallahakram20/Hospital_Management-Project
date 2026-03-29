using Hospital_Management_Project.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // ضع نص الاتصال الخاص بك هنا بشكل مؤقت لحل مشكلة الـ Migration
        optionsBuilder.UseSqlServer("Server=DESKTOP-UFO9921\\SQLEXPRESS;Database=Hospital_Management_Project;Trusted_Connection=True; TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}