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
using TaskZ.API.Models.Reports;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Principal;

namespace TaskZ.Tests.Tasks
{

    public class ModelsTests
    {
        [Fact]
        public void AddCommentRequest_ShouldSetProperties()
        {
            // Arrange
            var content = "This is a comment";
            var userId = Guid.NewGuid();

            // Act
            var request = new AddCommentRequest
            {
                Content = content,
                UserId = userId
            };

            // Assert
            Assert.Equal(content, request.Content);
            Assert.Equal(userId, request.UserId);
        }

        [Fact]
        public void CommentResponse_FromComment_ShouldMapProperties()
        {
            // Arrange
            var comment = new Comment("Test Comment", Guid.NewGuid(), Guid.NewGuid());

            // Act
            var response = CommentResponse.FromComment(comment);

            // Assert
            Assert.Equal(comment.Id, response.Id);
            Assert.Equal(comment.CreatedAt, response.CreatedAt);
            Assert.Equal(comment.Content, response.Content);
            Assert.Equal(comment.UserId, response.UserId);
        }

        [Fact]
        public void CreateTaskRequest_ShouldSetProperties()
        {
            // Arrange
            var title = "Task Title";
            var description = "Task Description";
            var dueDate = DateTime.UtcNow.AddDays(1);
            var priority = TaskPriority.High;
            var projectId = Guid.NewGuid();

            // Act
            var request = new CreateTaskRequest
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                Priority = priority,
                ProjectId = projectId
            };

            // Assert
            Assert.Equal(title, request.Title);
            Assert.Equal(description, request.Description);
            Assert.Equal(dueDate, request.DueDate);
            Assert.Equal(priority, request.Priority);
            Assert.Equal(projectId, request.ProjectId);
        }

        [Fact]
        public void TaskHistoryResponse_FromTaskHistory_ShouldMapProperties()
        {
            // Arrange
            var history = new TaskHistory("Task History Description", Guid.NewGuid(), Guid.NewGuid());

            // Act
            var response = TaskHistoryResponse.FromTaskHistory(history);

            // Assert
            Assert.Equal(history.Id, response.Id);
            Assert.Equal(history.Description, response.Description);
            Assert.Equal(history.Timestamp, response.Timestamp);
            Assert.Equal(history.UserId, response.UserId);
        }

        [Fact]
        public void TaskResponse_FromTask_ShouldMapProperties()
        {
            // Arrange
            var task = new Core.Entities.Task("Task Title", "Task Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, Guid.NewGuid());
            
            task.Comments.Add(new Comment("Test Comment", Guid.NewGuid(), Guid.NewGuid()));

            task.History.Add(new TaskHistory("Task History Description", Guid.NewGuid(), Guid.NewGuid()));

            // Act
            var response = TaskResponse.FromTask(task);

            // Assert
            Assert.Equal(task.Id, response.Id);
            Assert.Equal(task.Title, response.Title);
            Assert.Equal(task.Description, response.Description);
            Assert.Equal(task.DueDate, response.DueDate);
            Assert.Equal(task.Status, response.Status);
            Assert.Equal(task.Priority, response.Priority);
            Assert.Equal(task.Comments.Count, response.Comments.Count);
            Assert.Equal(task.History.Count, response.History.Count);
        }

        [Fact]
        public void UpdateTaskStatusRequest_ShouldSetProperties()
        {
            // Arrange
            var status = Core.Enums.TaskStatus.Completed;
            var userId = Guid.NewGuid();

            // Act
            var request = new UpdateTaskStatusRequest
            {
                Status = status,
                UserId = userId
            };

            // Assert
            Assert.Equal(status, request.Status);
            Assert.Equal(userId, request.UserId);
        }
    }

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
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public TaskRepositoryTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetProjectTasksAsync_ShouldReturnTasks_ForGivenProjectId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new TaskRepository(context);
            var projectId = Guid.NewGuid();
            context.Tasks.Add(new Core.Entities.Task("Task 1", "Description 1", DateTime.UtcNow.AddDays(1), Core.Enums.TaskPriority.Medium, projectId));
            context.Tasks.Add(new Core.Entities.Task("Task 2", "Description 2", DateTime.UtcNow.AddDays(2), Core.Enums.TaskPriority.High, projectId));
            context.Tasks.Add(new Core.Entities.Task("Task 3", "Description 3", DateTime.UtcNow.AddDays(3), Core.Enums.TaskPriority.Low, Guid.NewGuid()));
            await context.SaveChangesAsync();

            // Act
            var tasks = await repository.GetProjectTasksAsync(projectId);

            // Assert
            tasks.Should().NotBeNull();
            tasks.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnTask_WhenTaskExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new TaskRepository(context);
            var task = new Core.Entities.Task("Test Task", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Medium, Guid.NewGuid());
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(task.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(task.Id);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddTask_ToDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new TaskRepository(context);
            var task = new Core.Entities.Task("New Task", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.High, Guid.NewGuid());

            // Act
            await repository.CreateAsync(task);

            // Assert
            var createdTask = await context.Tasks.FindAsync(task.Id);
            createdTask.Should().NotBeNull();
            createdTask.Title.Should().Be("New Task");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyTask_InDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new TaskRepository(context);
            var task = new Core.Entities.Task("Task to Update", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Medium, Guid.NewGuid());
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            // Act
            task.UpdateStatus(Core.Enums.TaskStatus.Completed, Guid.NewGuid());
            await repository.UpdateAsync(task);

            // Assert
            var updatedTask = await context.Tasks.FindAsync(task.Id);
            updatedTask.Should().NotBeNull();
            updatedTask.Status.Should().Be(Core.Enums.TaskStatus.Completed);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTask_FromDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new TaskRepository(context);
            var task = new Core.Entities.Task("Task to Delete", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Low, Guid.NewGuid());
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            // Act
            await repository.DeleteAsync(task);

            // Assert
            var deletedTask = await context.Tasks.FindAsync(task.Id);
            deletedTask.Should().BeNull();
        }
    }

    public class TasksControllerTests
    {
        private readonly Mock<ITaskRepository> _mockTaskRepository;
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockProjectRepository = new Mock<IProjectRepository>();
            _controller = new TasksController(_mockTaskRepository.Object, _mockProjectRepository.Object);
        }

        [Fact]
        public async Task GetProjectTasks_ShouldReturnOk_WhenTasksExist()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var tasks = new List<Core.Entities.Task> { new Core.Entities.Task("Test Task", "Description", DateTime.UtcNow.AddDays(1), Core.Enums.TaskPriority.Medium, projectId) };
            _mockTaskRepository.Setup(repo => repo.GetProjectTasksAsync(projectId)).ReturnsAsync(tasks);

            // Act
            var result = await _controller.GetProjectTasks(projectId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            var returnedTasks = okResult.Value as List<TaskResponse>;
            returnedTasks.Should().NotBeNull();
            returnedTasks.Count.Should().Be(1);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenTaskIsValid()
        {
            // Arrange
            var request = new CreateTaskRequest
            {
                Title = "New Task",
                Description = "Task Description",
                DueDate = DateTime.UtcNow.AddDays(5),
                Priority = Core.Enums.TaskPriority.High,
                ProjectId = Guid.NewGuid()
            };
            var project = new Project("Test Project", "Description", request.ProjectId);
            _mockProjectRepository.Setup(repo => repo.GetByIdAsync(request.ProjectId)).ReturnsAsync(project);
            _mockTaskRepository.Setup(repo => repo.CreateAsync(It.IsAny<Core.Entities.Task>())).ReturnsAsync(new Core.Entities.Task(request.Title, request.Description, request.DueDate, request.Priority, request.ProjectId));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be(201);
            var returnedTask = createdResult.Value as TaskResponse;
            returnedTask.Should().NotBeNull();
            returnedTask.Title.Should().Be(request.Title);
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId)).ReturnsAsync((Core.Entities.Task)null);

            // Act
            var result = await _controller.UpdateStatus(taskId, new UpdateTaskStatusRequest { Status = Core.Enums.TaskStatus.Completed, UserId = Guid.NewGuid() });

            // Assert
            var notFoundResult = result as NotFoundResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnNoContent_WhenStatusIsUpdated()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new Core.Entities.Task("Test Task", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Medium, Guid.NewGuid());
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId)).ReturnsAsync(task);
            _mockTaskRepository.Setup(repo => repo.UpdateAsync(task)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateStatus(taskId, new UpdateTaskStatusRequest { Status = Core.Enums.TaskStatus.Completed, UserId = Guid.NewGuid() });

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
        }

        [Fact]
        public async Task AddComment_ShouldReturnOk_WhenCommentIsAdded()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new Core.Entities.Task("Test Task", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Medium, Guid.NewGuid());
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId)).ReturnsAsync(task);
            _mockTaskRepository.Setup(repo => repo.UpdateAsync(task)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddComment(taskId, new AddCommentRequest { Content = "New Comment", UserId = Guid.NewGuid() });

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            var commentResponse = okResult.Value as CommentResponse;
            commentResponse.Should().NotBeNull();
            commentResponse.Content.Should().Be("New Comment");
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId)).ReturnsAsync((Core.Entities.Task)null);

            // Act
            var result = await _controller.Delete(taskId);

            // Assert
            var notFoundResult = result as NotFoundResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenTaskIsDeleted()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new Core.Entities.Task("Test Task", "Description", DateTime.UtcNow.AddDays(5), Core.Enums.TaskPriority.Medium, Guid.NewGuid());
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId)).ReturnsAsync(task);
            _mockTaskRepository.Setup(repo => repo.DeleteAsync(task)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(taskId);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
        }

        [Fact]
        public async Task GetCompletionReport_ShouldReturnUnauthorized_WhenUserIsNotManager()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCompletionReport(userId, false);

            // Assert
            var unauthorizedResult = result.Result as UnauthorizedResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetCompletionReport_ShouldReturnOk_WhenUserIsManager()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockTaskRepository.Setup(repo => repo.GetCompletedTasksCountInLastDaysAsync(userId, 30)).ReturnsAsync(10);

            // Act
            var result = await _controller.GetCompletionReport(userId, true);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            var report = okResult.Value as TaskCompletionReport;
            report.Should().NotBeNull();
            report.CompletedTasksLast30Days.Should().Be(10);
            report.AverageTasksPerDay.Should().BeApproximately(10.0 / 30, 0.001);
        }
    }
}   
