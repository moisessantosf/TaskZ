using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using TaskZ.API.Controllers;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.API.Models.Projects;
using FluentAssertions;
using TaskZ.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using TaskZ.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace TaskZ.UnitTests.Controllers
{
    public class ProjectsControllerTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepository;
        private readonly Mock<ITaskRepository> _mockTaskRepository;
        private readonly ProjectsController _controller;

        public ProjectsControllerTests()
        {
            _mockProjectRepository = new Mock<IProjectRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _controller = new ProjectsController(_mockProjectRepository.Object, _mockTaskRepository.Object);
        }

        [Fact]
        public async Task GetUserProjects_ShouldReturnOk_WhenProjectsExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projects = new List<Project> { new Project("Test Project", "Description", userId) };
            _mockProjectRepository.Setup(repo => repo.GetUserProjectsAsync(userId)).ReturnsAsync(projects);

            // Act
            var result = await _controller.GetUserProjects(userId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
            var returnedProjects = okResult.Value as List<ProjectResponse>;
            returnedProjects.Should().NotBeNull();
            returnedProjects.Count.Should().Be(1);
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WhenProjectIsValid()
        {
            // Arrange
            var request = new CreateProjectRequest
            {
                Name = "New Project",
                Description = "Project Description",
                UserId = Guid.NewGuid()
            };
            var project = new Project(request.Name, request.Description, request.UserId);
            _mockProjectRepository.Setup(repo => repo.CreateAsync(It.IsAny<Project>())).ReturnsAsync(project);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();
            createdResult.StatusCode.Should().Be(201);
            var returnedProject = createdResult.Value as ProjectResponse;
            returnedProject.Should().NotBeNull();
            returnedProject.Name.Should().Be(request.Name);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenProjectDoesNotExist()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            _mockProjectRepository.Setup(repo => repo.GetByIdAsync(projectId)).ReturnsAsync((Project)null);

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            var notFoundResult = result as NotFoundResult;
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Delete_ShouldReturnBadRequest_WhenProjectCannotBeDeleted()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            project.Tasks.Add(new Core.Entities.Task("Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));
            _mockProjectRepository.Setup(repo => repo.GetByIdAsync(projectId)).ReturnsAsync(project);

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().Be("Cannot delete project with pending tasks. Please complete or remove all tasks first.");
        }

        [Fact]
        public async Task Delete_ShouldReturnNoContent_WhenProjectIsDeleted()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            _mockProjectRepository.Setup(repo => repo.GetByIdAsync(projectId)).ReturnsAsync(project);
            _mockProjectRepository.Setup(repo => repo.DeleteAsync(project)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            var noContentResult = result as NoContentResult;
            noContentResult.Should().NotBeNull();
            noContentResult.StatusCode.Should().Be(204);
        }
    }
}

namespace TaskZ.UnitTests.Repositories
{
    public class ProjectRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public ProjectRepositoryTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetUserProjectsAsync_ShouldReturnProjects_ForGivenUserId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new ProjectRepository(context);
            var userId = Guid.NewGuid();
            context.Projects.Add(new Project("Test Project 1", "Description 1", userId));
            context.Projects.Add(new Project("Test Project 2", "Description 2", userId));
            context.Projects.Add(new Project("Other Project", "Description", Guid.NewGuid()));
            await context.SaveChangesAsync();

            // Act
            var projects = await repository.GetUserProjectsAsync(userId);

            // Assert
            projects.Should().NotBeNull();
            projects.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProject_WhenProjectExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new ProjectRepository(context);
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(project.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(project.Id);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddProject_ToDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new ProjectRepository(context);
            var project = new Project("New Project", "Description", Guid.NewGuid());

            // Act
            await repository.CreateAsync(project);

            // Assert
            var createdProject = await context.Projects.FindAsync(project.Id);
            createdProject.Should().NotBeNull();
            createdProject.Name.Should().Be("New Project");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveProject_FromDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var repository = new ProjectRepository(context);
            var project = new Project("Project to Delete", "Description", Guid.NewGuid());
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            // Act
            await repository.DeleteAsync(project);

            // Assert
            var deletedProject = await context.Projects.FindAsync(project.Id);
            deletedProject.Should().BeNull();
        }
    }
}

namespace TaskZ.UnitTests.Entities
{
    public class ProjectEntityTests
    {
        [Fact]
        public void CanBeDeleted_ShouldReturnTrue_WhenAllTasksAreCompleted()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            project.Tasks.Add(new Core.Entities.Task("Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));
            project.Tasks.Add(new Core.Entities.Task("Task 2", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));

            project.Tasks[0].UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);
            project.Tasks[1].UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);

            // Act
            var result = project.CanBeDeleted();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CanBeDeleted_ShouldReturnFalse_WhenThereArePendingTasks()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            project.Tasks.Add(new Core.Entities.Task("Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));
            project.Tasks.Add(new Core.Entities.Task("Task 2", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));

            project.Tasks[1].UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);

            // Act
            var result = project.CanBeDeleted();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CanAddTask_ShouldReturnTrue_WhenTaskCountIsLessThanLimit()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            for (int i = 0; i < 19; i++)
            {
                project.Tasks.Add(new Core.Entities.Task($"Task {i}", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));
            }

            // Act
            var result = project.CanAddTask();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CanAddTask_ShouldReturnFalse_WhenTaskCountIsAtLimit()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            for (int i = 0; i < 20; i++)
            {
                project.Tasks.Add(new Core.Entities.Task($"Task {i}", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id));
            }

            // Act
            var result = project.CanAddTask();

            // Assert
            result.Should().BeFalse();
        }
    }
}


/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskZ.API.Controllers;
using TaskZ.API.Models.Projects;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.Core.Enums;
using TaskZ.Infrastructure.Data;
using TaskZ.Infrastructure.Repositories;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskZ.Tests
{
    public class ProjectsControllerTests
    {
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly ProjectsController _controller;

        public ProjectsControllerTests()
        {
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _controller = new ProjectsController(_projectRepositoryMock.Object, _taskRepositoryMock.Object);
        }

        [Fact]
        public async Task GetUserProjects_ReturnsOkResult_WithProjectList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var projects = new List<Project>
            {
                new Project("Project 1", "Description 1", userId),
                new Project("Project 2", "Description 2", userId)
            };

            _projectRepositoryMock.Setup(repo => repo.GetUserProjectsAsync(userId))
                .ReturnsAsync(projects);

            // Act
            var result = await _controller.GetUserProjects(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProjects = Assert.IsAssignableFrom<List<ProjectResponse>>(okResult.Value);
            Assert.Equal(2, returnedProjects.Count);
            Assert.Equal(projects[0].Name, returnedProjects[0].Name);
            Assert.Equal(projects[1].Name, returnedProjects[1].Name);
        }

        [Fact]
        public async Task GetUserProjects_WithNoProjects_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _projectRepositoryMock.Setup(repo => repo.GetUserProjectsAsync(userId))
                .ReturnsAsync(new List<Project>());

            // Act
            var result = await _controller.GetUserProjects(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedProjects = Assert.IsAssignableFrom<List<ProjectResponse>>(okResult.Value);
            Assert.Empty(returnedProjects);
        }

        [Fact]
        public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new CreateProjectRequest
            {
                Name = "New Project",
                Description = "New Description",
                UserId = Guid.NewGuid()
            };

            _projectRepositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<Project>()))
                .ReturnsAsync((Project project) => project);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var projectResponse = Assert.IsType<ProjectResponse>(createdAtActionResult.Value);
            Assert.Equal(request.Name, projectResponse.Name);
            Assert.Equal(request.Description, projectResponse.Description);
            _projectRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Project>()), Times.Once);
        }

        [Fact]
        public async Task Delete_WithNonExistentProject_ReturnsNotFound()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            _projectRepositoryMock.Setup(repo => repo.GetByIdAsync(projectId))
                .ReturnsAsync((Project)null);

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _projectRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<Project>()), Times.Never);
        }

        [Fact]
        public async Task Delete_WithPendingTasks_ReturnsBadRequest()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            var task = new Core.Entities.Task("Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
            project.Tasks.Add(task);

            _projectRepositoryMock.Setup(repo => repo.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            // Act
            var result = await _controller.Delete(projectId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cannot delete project with pending tasks. Please complete or remove all tasks first.",
                badRequestResult.Value);
            _projectRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<Project>()), Times.Never);
        }
    }

    public class ProjectTests
    {
        [Fact]
        public void Constructor_InitializesProjectCorrectly()
        {
            // Arrange
            var name = "Test Project";
            var description = "Test Description";
            var userId = Guid.NewGuid();

            // Act
            var project = new Project(name, description, userId);

            // Assert
            Assert.Equal(name, project.Name);
            Assert.Equal(description, project.Description);
            Assert.Equal(userId, project.UserId);
            Assert.NotEqual(Guid.Empty, project.Id);
            Assert.True(DateTime.UtcNow >= project.CreatedAt);
            Assert.Empty(project.Tasks);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(19)]
        public void CanAddTask_WithLessThan20Tasks_ReturnsTrue(int taskCount)
        {
            // Arrange
            var project = new Project("Test", "Description", Guid.NewGuid());
            for (int i = 0; i < taskCount; i++)
            {
                var task = new Core.Entities.Task($"Task {i}", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
                project.Tasks.Add(task);
            }

            // Act
            var result = project.CanAddTask();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanAddTask_With20Tasks_ReturnsFalse()
        {
            // Arrange
            var project = new Project("Test", "Description", Guid.NewGuid());
            for (int i = 0; i < 20; i++)
            {
                var task = new Core.Entities.Task($"Task {i}", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
                project.Tasks.Add(task);
            }

            // Act
            var result = project.CanAddTask();

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(5, false)]
        public void CanBeDeleted_WithPendingTaskCount_ReturnsExpectedResult(int pendingTaskCount, bool expectedResult)
        {
            // Arrange
            var project = new Project("Test", "Description", Guid.NewGuid());
            for (int i = 0; i < pendingTaskCount; i++)
            {
                var task = new Core.Entities.Task($"Task {i}", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
                project.Tasks.Add(task);
            }

            // Act
            var result = project.CanBeDeleted();

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }

    public class ProjectRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private readonly ApplicationDbContext _context;
        private readonly ProjectRepository _repository;

        public ProjectRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(_options);
            _repository = new ProjectRepository(_context);
        }

        [Fact]
        public async Task GetUserProjectsAsync_ReturnsUserProjects()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            await _context.Projects.AddRangeAsync(
                new Project("Project 1", "Description 1", userId),
                new Project("Project 2", "Description 2", userId),
                new Project("Other Project", "Description 3", otherUserId)
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetUserProjectsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(userId, p.UserId));
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingProject_ReturnsProject()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(project.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(project.Id, result.Id);
            Assert.Equal(project.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentProject_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_AddsProjectToDatabase()
        {
            // Arrange
            var project = new Project("New Project", "Description", Guid.NewGuid());

            // Act
            var result = await _repository.CreateAsync(project);

            // Assert
            Assert.NotNull(result);
            var dbProject = await _context.Projects.FindAsync(project.Id);
            Assert.NotNull(dbProject);
            Assert.Equal(project.Name, dbProject.Name);
        }

        [Fact]
        public async Task DeleteAsync_RemovesProjectFromDatabase()
        {
            // Arrange
            var project = new Project("Test Project", "Description", Guid.NewGuid());
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(project);

            // Assert
            var dbProject = await _context.Projects.FindAsync(project.Id);
            Assert.Null(dbProject);
        }
    }

    public class ProjectResponseTests
    {
        [Fact]
        public void FromProject_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var project = new Project("Test Project", "Test Description", Guid.NewGuid());
            var task1 = new Core.Entities.Task($"Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
            var task2 = new Core.Entities.Task($"Task 2", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);

            task2.UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);

            project.Tasks.Add(task1);
            project.Tasks.Add(task2);

            // Act
            var response = ProjectResponse.FromProject(project);

            // Assert
            Assert.Equal(project.Id, response.Id);
            Assert.Equal(project.Name, response.Name);
            Assert.Equal(project.Description, response.Description);
            Assert.Equal(project.CreatedAt, response.CreatedAt);
            Assert.Equal(2, response.TaskCount);
            Assert.Equal(1, response.CompletedTaskCount);
        }

        [Fact]
        public void FromProject_WithNoTasks_ReturnsZeroCounts()
        {
            // Arrange
            var project = new Project("Test Project", "Test Description", Guid.NewGuid());

            // Act
            var response = ProjectResponse.FromProject(project);

            // Assert
            Assert.Equal(0, response.TaskCount);
            Assert.Equal(0, response.CompletedTaskCount);
        }

        [Fact]
        public void FromProject_WithAllCompletedTasks_ReturnsEqualCounts()
        {
            // Arrange
            var project = new Project("Test Project", "Test Description", Guid.NewGuid());
            var task1 = new Core.Entities.Task($"Task 1", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
            var task2 = new Core.Entities.Task($"Task 2", "Description", DateTime.UtcNow, Core.Enums.TaskPriority.Low, project.Id);
           
            task1.UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);
            task2.UpdateStatus(Core.Enums.TaskStatus.Completed, project.UserId);

            project.Tasks.Add(task1);
            project.Tasks.Add(task2);

            // Act
            var response = ProjectResponse.FromProject(project);

            // Assert
            Assert.Equal(2, response.TaskCount);
            Assert.Equal(2, response.CompletedTaskCount);
        }
    }
}
*/