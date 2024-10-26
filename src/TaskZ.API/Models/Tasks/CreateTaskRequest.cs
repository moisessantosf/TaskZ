using TaskZ.Core.Enums;

namespace TaskZ.API.Models.Tasks
{
    public class CreateTaskRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskPriority Priority { get; set; }
        public Guid ProjectId { get; set; }
    }
}
