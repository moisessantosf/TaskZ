namespace TaskZ.Core.Entities
{
    public class TaskHistory
    {
        public Guid Id { get; private set; }
        public string Description { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Guid TaskId { get; private set; }
        public Guid? UserId { get; private set; }

        private TaskHistory() { }

        public TaskHistory(string description, Guid taskId, Guid? userId = null)
        {
            Id = Guid.NewGuid();
            Description = description;
            TaskId = taskId;
            UserId = userId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
