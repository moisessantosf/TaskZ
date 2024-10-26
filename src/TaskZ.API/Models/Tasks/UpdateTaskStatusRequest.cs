using TaskStatus = TaskZ.Core.Enums.TaskStatus;

namespace TaskZ.API.Models.Tasks
{
    public class UpdateTaskStatusRequest
    {
        public TaskStatus Status { get; set; }
        public Guid UserId { get; set; }
    }
}
