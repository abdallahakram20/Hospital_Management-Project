namespace Hospital_Management_Project.Models
{
    public enum AppointmentStatus
    {
        Available = 0,      // متاح للحجز
        Booked = 1,         // محجوز
        Completed = 2,      // انتهى
        Cancelled = 3,      // ملغى
        NoShow = 4          // لم يحضر
    }
}
