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
        private readonly ProjectsController _controller;

        public ProjectsControllerTests()
        {
            _mockProjectRepository = new Mock<IProjectRepository>();
            _controller = new ProjectsController(_mockProjectRepository.Object);
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
            _ = _mockProjectRepository.Setup(repo => repo.GetByIdAsync(projectId)).ReturnsAsync((Project)null);

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