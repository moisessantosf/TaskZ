namespace TaskZ.Core.Entities
{
    public class Project
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public List<Task> Tasks { get; private set; } = new();

        private Project() { }

        public Project(string name, string description, Guid userId)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
        }

        public bool CanBeDeleted()
        {
            return !Tasks.Any(t => t.Status != Enums.TaskStatus.Completed);
        }

        public bool CanAddTask()
        {
            return Tasks.Count < 20;
        }
    }
}