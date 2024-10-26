namespace TaskZ.Core.Interfaces
{
    public interface ITaskRepository
    {
        Task<List<Entities.Task>> GetProjectTasksAsync(Guid projectId);
        Task<Entities.Task> GetByIdAsync(Guid id);
        Task<Entities.Task> CreateAsync(Entities.Task task);
        Task UpdateAsync(Entities.Task task);
        Task DeleteAsync(Entities.Task task);
        Task<int> GetCompletedTasksCountInLastDaysAsync(Guid userId, int days);
    }
}
