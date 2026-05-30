using System;

namespace Hospital_Management_Project.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? EntityType { get; set; }
        public int EntityId { get; set; }
        public string? Action { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime Timestamp { get; set; }
    }
}