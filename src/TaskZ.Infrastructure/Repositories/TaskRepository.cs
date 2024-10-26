using Microsoft.EntityFrameworkCore;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace TaskZ.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Core.Entities.Task>> GetProjectTasksAsync(Guid projectId)
        {
            return await _context.Tasks
                .Include(t => t.History)
                .Include(t => t.Comments)
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<Core.Entities.Task> GetByIdAsync(Guid id)
        {
            return await _context.Tasks
                .Include(t => t.History)
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Core.Entities.Task> CreateAsync(Core.Entities.Task task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task UpdateAsync(Core.Entities.Task task)
        {

            var entry = _context.Entry(task);

            if (entry.State == EntityState.Detached)
            {
                _context.Tasks.Attach(task);
            }

            entry.State = EntityState.Modified;

            var existingHistoryIds = await _context.Set<TaskHistory>()
                .Where(h => h.TaskId == task.Id)
                .Select(h => h.Id)
                .ToListAsync();

            var newHistories = task.History
                .Where(h => !existingHistoryIds.Contains(h.Id))
                .ToList();

            foreach (var history in newHistories)
            {
                _context.Entry(history).State = EntityState.Added;
            }

            var existingCommentsIds = await _context.Set<Comment>()
                .Where(h => h.TaskId == task.Id)
                .Select(h => h.Id)
                .ToListAsync();

            var newComments = task.Comments
                .Where(h => !existingCommentsIds.Contains(h.Id))
                .ToList();

            foreach (var comment in newComments)
            {
                _context.Entry(comment).State = EntityState.Added;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Core.Entities.Task task)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCompletedTasksCountInLastDaysAsync(Guid userId, int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Tasks
                .Include(t => t.History)
                .Where(t => t.Status == Core.Enums.TaskStatus.Completed)
                .Where(t => t.History.Any(h => h.UserId == userId && h.Timestamp >= cutoffDate))
                .CountAsync();
        }
    }
}
