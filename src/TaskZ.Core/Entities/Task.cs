using TaskZ.Core.Enums;

namespace TaskZ.Core.Entities
{
    public class Task
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public DateTime DueDate { get; private set; }
        public Enums.TaskStatus Status { get; private set; }
        public TaskPriority Priority { get; private set; }
        public Guid ProjectId { get; private set; }
        public List<TaskHistory> History { get; private set; } = new();
        public List<Comment> Comments { get; private set; } = new();

        private Task() { } 

        public Task(string title, string description, DateTime dueDate, TaskPriority priority, Guid projectId)
        {
            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            DueDate = dueDate;
            Status = Enums.TaskStatus.Pending;
            Priority = priority;
            ProjectId = projectId;

            AddHistory("Task created");
        }

        public void UpdateStatus(Enums.TaskStatus newStatus, Guid userId)
        {
            if (Status != newStatus)
            {
                Status = newStatus;
                AddHistory($"Status changed to {newStatus}", userId);
            }
        }

        public void AddComment(string content, Guid userId)
        {
            var comment = new Comment(content, userId, Id);
            Comments.Add(comment);
            AddHistory($"Comment added: {content}", userId);
        }

        private void AddHistory(string description, Guid? userId = null)
        {
            History.Add(new TaskHistory(description, Id, userId));
        }
    }
}
