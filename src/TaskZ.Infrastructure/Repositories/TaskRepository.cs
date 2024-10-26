using Microsoft.EntityFrameworkCore;
using TaskZ.Core.Interfaces;
using TaskZ.Infrastructure.Data;

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

            // Verifica se a entidade já está sendo rastreada
            var entry = _context.Entry(task);

            if (entry.State == EntityState.Detached)
            {
                // Se não estiver sendo rastreada, anexa ao contexto
                _context.Tasks.Attach(task);
            }

            // Marca a entidade como modificada
            entry.State = EntityState.Modified;

            // Marca as coleções como modificadas
            foreach (var history in task.History)
            {
                if (_context.Entry(history).State != EntityState.Unchanged)
                {
                    _context.Entry(history).State = EntityState.Added;
                }
            }

            foreach (var comment in task.Comments)
            {
                if (_context.Entry(comment).State != EntityState.Unchanged)
                {
                    _context.Entry(comment).State = EntityState.Added;
                }
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
