using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Server.Data;
using TaskManagement.Server.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManagement.Server.Tests
{

    public class TasksControllerTests
    {
        [Fact]
        public async Task GetTasks_WithoutAuthentication_Returns401()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/tasks");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTasks_ReturnsOnlyCurrentUsersTasks()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userA = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            var userB = Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

            await SeedTasks(factory, userA, userB);

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userA.ToString());

            var response = await client.GetAsync("/api/tasks");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var tasks = await response.Content
                .ReadFromJsonAsync<List<TaskItem>>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new JsonStringEnumConverter()
                        }
                    });

            Assert.NotNull(tasks);
            Assert.Single(tasks);

            Assert.Equal("Task A", tasks[0].Title);
        }

        private static async Task SeedTasks(
            CustomWebApplicationFactory factory,
            Guid userA,
            Guid userB)
        {
            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<TaskDbContext>();

            context.Tasks.AddRange(
                new TaskItem
                {
                    UserId = userA,
                    Title = "Task A",
                    Description = "User A task",
                    Status = Models.TaskStatus.NotStarted
                },
                new TaskItem
                {
                    UserId = userB,
                    Title = "Task B",
                    Description = "User B task",
                    Status = Models.TaskStatus.InProgress
                });

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateTask_WithAuthentication_CreatesTask()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userId.ToString());

            var request = new CreateTaskRequest
            {
                Title = "New task",
                Description = "Test description"
            };

            var response = await client.PostAsJsonAsync(
                "/api/tasks",
                request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var task = await response.Content
                .ReadFromJsonAsync<TaskItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                    new JsonStringEnumConverter()
                        }
                    });

            Assert.NotNull(task);

            Assert.NotEqual(0, task.Id);
            Assert.Equal("New task", task.Title);
            Assert.Equal("Test description", task.Description);
            Assert.Equal(userId, task.UserId);
            Assert.Equal(Models.TaskStatus.NotStarted, task.Status);
        }

        [Fact]
        public async Task UpdateTaskStatus_WithAuthentication_UpdatesTask()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            await SeedTasks(
                factory,
                userId,
                Guid.Parse("22222222-2222-2222-2222-222222222222"));

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userId.ToString());

            var tasks = await client.GetFromJsonAsync<List<TaskItem>>(
                "/api/tasks",
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                new JsonStringEnumConverter()
                    }
                });

            Assert.NotNull(tasks);
            Assert.Single(tasks);

            var taskId = tasks[0].Id;

            var request = new UpdateTaskStatusRequest
            {
                Status = Models.TaskStatus.Completed
            };

            var response = await client.PatchAsJsonAsync(
                $"/api/tasks/{taskId}/status",
                request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updatedTask = await response.Content
                .ReadFromJsonAsync<TaskItem>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                    new JsonStringEnumConverter()
                        }
                    });

            Assert.NotNull(updatedTask);
            Assert.Equal(Models.TaskStatus.Completed, updatedTask.Status);
        }

        [Fact]
        public async Task UpdateTaskStatus_ForAnotherUsersTask_Returns404()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userA = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            var userB = Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

            await SeedTasks(factory, userA, userB);

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userA.ToString());

            var context = factory.Services
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<TaskDbContext>();

            var userBTask = context.Tasks
                .Single(x => x.UserId == userB);

            var request = new UpdateTaskStatusRequest
            {
                Status = Models.TaskStatus.Completed
            };

            var response = await client.PatchAsJsonAsync(
                $"/api/tasks/{userBTask.Id}/status",
                request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTask_WithAuthentication_DeletesTask()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userId = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            await SeedTasks(
                factory,
                userId,
                Guid.Parse("22222222-2222-2222-2222-222222222222"));

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userId.ToString());

            var context = factory.Services
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<TaskDbContext>();

            var task = context.Tasks
                .Single(x => x.UserId == userId);

            var response = await client.DeleteAsync(
                $"/api/tasks/{task.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var deletedTask = context.Tasks
                .SingleOrDefault(x => x.Id == task.Id);

            Assert.Null(deletedTask);
        }

        [Fact]
        public async Task DeleteTask_ForAnotherUsersTask_Returns404()
        {
            await using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var userA = Guid.Parse(
                "11111111-1111-1111-1111-111111111111");

            var userB = Guid.Parse(
                "22222222-2222-2222-2222-222222222222");

            await SeedTasks(factory, userA, userB);

            client.DefaultRequestHeaders.Add(
                "X-Test-User-Id",
                userA.ToString());

            var context = factory.Services
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<TaskDbContext>();

            var userBTask = context.Tasks
                .Single(x => x.UserId == userB);

            var response = await client.DeleteAsync(
                $"/api/tasks/{userBTask.Id}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        
    }
}