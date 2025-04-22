using Microsoft.AspNetCore.Mvc;
using TaskZ.API.Models.Projects;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;

namespace TaskZ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;

        public ProjectsController(IProjectRepository projectRepository)
        {
            _projectRepository = projectRepository;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<ProjectResponse>>> GetUserProjects(Guid userId)
        {
            var projects = await _projectRepository.GetUserProjectsAsync(userId);
            return Ok(projects.Select(ProjectResponse.FromProject).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponse>> Create([FromBody] CreateProjectRequest request)
        {
            var project = new Project(request.Name, request.Description, request.UserId);
            await _projectRepository.CreateAsync(project);
            return CreatedAtAction(
                nameof(GetUserProjects),
                new { userId = request.UserId },
                ProjectResponse.FromProject(project));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id);
            if (project == null)
                return NotFound();

            if (!project.CanBeDeleted())
                return BadRequest("Cannot delete project with pending tasks. Please complete or remove all tasks first.");

            await _projectRepository.DeleteAsync(project);
            return NoContent();
        }
    }
}
