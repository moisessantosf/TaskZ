
namespace TaskZ.API.Models.Tasks
{
    public class TaskResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public Core.Enums.TaskStatus Status { get; set; }
        public Core.Enums.TaskPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CommentResponse> Comments { get; set; }
        public List<TaskHistoryResponse> History { get; set; }

        public static TaskResponse FromTask(Core.Entities.Task task)
        {
            return new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                Priority = task.Priority,
                Comments = task.Comments.Select(CommentResponse.FromComment).ToList(),
                History = task.History.Select(TaskHistoryResponse.FromTaskHistory).ToList()
            };
        }
    }
}
