using TaskZ.Core.Entities;
using Task = System.Threading.Tasks.Task;

namespace TaskZ.Core.Interfaces
{
    public interface IProjectRepository
    {
        Task<List<Project>> GetUserProjectsAsync(Guid userId);
        Task<Project> GetByIdAsync(Guid id);
        Task<Project> CreateAsync(Project project);
        Task UpdateAsync(Project project);
        Task DeleteAsync(Project project);
    }
}
