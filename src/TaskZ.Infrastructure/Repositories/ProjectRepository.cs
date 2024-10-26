using Microsoft.EntityFrameworkCore;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace TaskZ.Infrastructure.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> GetUserProjectsAsync(Guid userId)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<Project> GetByIdAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task UpdateAsync(Project project)
        {
            _context.Entry(project).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Project project)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
    }
}
