using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.Core.Enums;
using TaskZ.Infrastructure.Repositories;
using TaskZ.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;
using TaskZ.API.Models.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskZ.API.Controllers;

namespace TaskZ.Tests
{
    public class TaskEntityTests
    {
        [Fact]
        public void CreateTask_WithValidData_ShouldCreateTaskCorrectly()
        {
            // Arrange
            var title = "Test Task";
            var description = "Test Description";
            var dueDate = DateTime.UtcNow.AddDays(1);
            var priority = TaskPriority.High;
            var projectId = Guid.NewGuid();

            // Act
            var task = new Core.Entities.Task(title, description, dueDate, priority, projectId);

            // Assert
            task.Should().NotBeNull();
            task.Title.Should().Be(title);
            task.Description.Should().Be(description);
            task.DueDate.Should().Be(dueDate);
            task.Priority.Should().Be(priority);
            task.ProjectId.Should().Be(projectId);
            task.Status.Should().Be(Core.Enums.TaskStatus.Pending);
            task.History.Should().HaveCount(1);
            task.History.First().Description.Should().Contain("Task created");
        }

        [Fact]
        public void UpdateStatus_WithNewStatus_ShouldUpdateStatusAndAddHistory()
        {
            // Arrange
            var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, Guid.NewGuid());
            var userId = Guid.NewGuid();
            var newStatus = Core.Enums.TaskStatus.InProgress;

            // Act
            task.UpdateStatus(newStatus, userId);

            // Assert
            task.Status.Should().Be(newStatus);
            task.History.Should().HaveCount(2);
            task.History.Last().Description.Should().Contain($"Status changed to {newStatus}");
            task.History.Last().UserId.Should().Be(userId);
        }

        [Fact]
        public void AddComment_ShouldAddCommentAndHistory()
        {
            // Arrange
            var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, Guid.NewGuid());
            var userId = Guid.NewGuid();
            var content = "Test comment";

            // Act
            task.AddComment(content, userId);

            // Assert
            task.Comments.Should().HaveCount(1);
            task.Comments.First().Content.Should().Be(content);
            task.Comments.First().UserId.Should().Be(userId);
            task.History.Should().HaveCount(2);
            task.History.Last().Description.Should().Contain("Comment added");
        }
    }

    public class TaskRepositoryTests
    {
        private DbContextOptions<ApplicationDbContext> GetInMemoryDbOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async System.Threading.Tasks.Task GetProjectTasksAsync_ShouldReturnTasksForProject()
        {
            // Arrange
            var options = GetInMemoryDbOptions();
            var projectId = Guid.NewGuid();

            using (var context = new ApplicationDbContext(options))
            {
                var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, projectId);
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new ApplicationDbContext(options))
            {
                var repository = new TaskRepository(context);
                var result = await repository.GetProjectTasksAsync(projectId);

                // Assert
                result.Should().HaveCount(1);
                result.First().ProjectId.Should().Be(projectId);
            }
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateTaskCorrectly()
        {
            // Arrange
            var options = GetInMemoryDbOptions();
            var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, Guid.NewGuid());

            using (var context = new ApplicationDbContext(options))
            {
                context.Tasks.Add(task);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new ApplicationDbContext(options))
            {
                var repository = new TaskRepository(context);
                task.UpdateStatus(Core.Enums.TaskStatus.InProgress, Guid.NewGuid());
                await repository.UpdateAsync(task);
            }

            // Assert
            using (var context = new ApplicationDbContext(options))
            {
                var updatedTask = await context.Tasks
                    .Include(t => t.History)
                    .FirstOrDefaultAsync(t => t.Id == task.Id);

                updatedTask.Should().NotBeNull();
                updatedTask.Status.Should().Be(Core.Enums.TaskStatus.InProgress);
                updatedTask.History.Should().HaveCount(2);
            }
        }
    }

    public class TasksControllerTests
    {
        private readonly Mock<ITaskRepository> _mockTaskRepo;
        private readonly Mock<IProjectRepository> _mockProjectRepo;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _mockTaskRepo = new Mock<ITaskRepository>();
            _mockProjectRepo = new Mock<IProjectRepository>();
            _controller = new TasksController(_mockTaskRepo.Object, _mockProjectRepo.Object);
        }

        [Fact]
        public async Task UpdateStatus_WithValidData_ShouldReturnNoContent()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, Guid.NewGuid());

            var request = new UpdateTaskStatusRequest
            {
                Status = Core.Enums.TaskStatus.InProgress,
                UserId = userId
            };

            _mockTaskRepo.Setup(r => r.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.UpdateStatus(taskId, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockTaskRepo.Verify(r => r.UpdateAsync(task), Times.Once);
        }

        //[Fact]
        //public async Task Create_WithValidRequest_ShouldReturnCreatedTask()
        //{
        //    // Arrange
        //    var projectId = Guid.NewGuid();
        //    var request = new CreateTaskRequest
        //    {
        //        Title = "Test Task",
        //        Description = "Test Description",
        //        DueDate = DateTime.UtcNow.AddDays(1),
        //        Priority = TaskPriority.High,
        //        ProjectId = projectId
        //    };

        //    var project = new Project(); // Você precisa implementar esta classe
        //    _mockProjectRepo.Setup(r => r.GetByIdAsync(projectId))
        //        .ReturnsAsync(project);

        //    // Act
        //    var result = await _controller.Create(request);

        //    // Assert
        //    var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        //    var taskResponse = createdResult.Value.Should().BeOfType<TaskResponse>().Subject;
        //    taskResponse.Title.Should().Be(request.Title);
        //    taskResponse.ProjectId.Should().Be(projectId);
        //}

        [Fact]
        public async Task AddComment_WithValidRequest_ShouldReturnComment()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var task = new Core.Entities.Task("Test", "Description", DateTime.UtcNow.AddDays(1), TaskPriority.Medium, Guid.NewGuid());

            var request = new AddCommentRequest
            {
                Content = "Test Comment",
                UserId = userId
            };

            _mockTaskRepo.Setup(r => r.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.AddComment(taskId, request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var comment = okResult.Value.Should().BeOfType<CommentResponse>().Subject;
            comment.Content.Should().Be(request.Content);
        }
    }
}