using TaskZ.Core.Entities;

namespace TaskZ.API.Models.Projects
{
    public class ProjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }

        public static ProjectResponse FromProject(Project project)
        {
            return new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                TaskCount = project.Tasks.Count,
                CompletedTaskCount = project.Tasks.Count(t => t.Status == Core.Enums.TaskStatus.Completed)
            };
        }
    }
}
