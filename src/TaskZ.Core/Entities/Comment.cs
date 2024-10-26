namespace TaskZ.Core.Entities
{
    public class Comment
    {
        public Guid Id { get; private set; }
        public string Content { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Guid UserId { get; private set; }
        public Guid TaskId { get; private set; }

        private Comment() { }

        public Comment(string content, Guid userId, Guid taskId)
        {
            Id = Guid.NewGuid();
            Content = content;
            UserId = userId;
            TaskId = taskId;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
