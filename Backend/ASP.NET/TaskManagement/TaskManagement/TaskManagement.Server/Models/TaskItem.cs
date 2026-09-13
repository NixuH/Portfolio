namespace TaskManagement.Server.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
    }
    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed
    }
}

