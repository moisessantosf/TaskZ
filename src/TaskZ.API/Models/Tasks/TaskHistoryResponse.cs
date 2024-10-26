using TaskZ.Core.Entities;

namespace TaskZ.API.Models.Tasks
{
    public class TaskHistoryResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid? UserId { get; set; }

        public static TaskHistoryResponse FromTaskHistory(TaskHistory history)
        {
            return new TaskHistoryResponse
            {
                Id = history.Id,
                Description = history.Description,
                Timestamp = history.Timestamp,
                UserId = history.UserId
            };
        }
    }
}
