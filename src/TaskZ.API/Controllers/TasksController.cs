using Microsoft.AspNetCore.Mvc;
using TaskZ.API.Models.Reports;
using TaskZ.API.Models.Tasks;
using TaskZ.Core.Interfaces;

namespace TaskZ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;

        public TasksController(ITaskRepository taskRepository, IProjectRepository projectRepository)
        {
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<TaskResponse>>> GetProjectTasks(Guid projectId)
        {
            var tasks = await _taskRepository.GetProjectTasksAsync(projectId);
            return Ok(tasks.Select(TaskResponse.FromTask).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request)
        {
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
                return NotFound("Project not found");

            if (!project.CanAddTask())
                return BadRequest("Project has reached the maximum number of tasks (20)");

            var task = new Core.Entities.Task(
                request.Title,
                request.Description,
                request.DueDate,
                request.Priority,
                request.ProjectId
            );

            await _taskRepository.CreateAsync(task);
            return CreatedAtAction(
                nameof(GetProjectTasks),
                new { projectId = request.ProjectId },
                TaskResponse.FromTask(task));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            task.UpdateStatus(request.Status, request.UserId);
            await _taskRepository.UpdateAsync(task);
            return NoContent();
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<CommentResponse>> AddComment(Guid id, [FromBody] AddCommentRequest request)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            task.AddComment(request.Content, request.UserId);
            await _taskRepository.UpdateAsync(task);

            var comment = task.Comments.Last();
            return Ok(CommentResponse.FromComment(comment));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            await _taskRepository.DeleteAsync(task);
            return NoContent();
        }

        [HttpGet("reports/completion")]
        public async Task<ActionResult<TaskCompletionReport>> GetCompletionReport([FromQuery] Guid userId, [FromQuery] bool isManager)
        {
            if (!isManager)
                return Unauthorized();

            var completedTasksCount = await _taskRepository.GetCompletedTasksCountInLastDaysAsync(userId, 30);
            var report = new TaskCompletionReport
            {
                UserId = userId,
                CompletedTasksLast30Days = completedTasksCount,
                AverageTasksPerDay = (double)completedTasksCount / 30
            };

            return Ok(report);
        }
    }
}
