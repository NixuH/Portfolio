using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.Server.Data;
using TaskManagement.Server.Models;

namespace TaskManagement.Server.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly TaskDbContext _context;
        public TasksController(TaskDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var tasks = await _context.Tasks
                .Where(x => x.UserId == Guid.Parse(userId))
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == Guid.Parse(userId));

            return task == null
                ? NotFound("Task not found")
                : Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(CreateTaskRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var task = new TaskItem
            {
                UserId = Guid.Parse(userId),
                Title = request.Title,
                Description = request.Description,
                Status = Models.TaskStatus.NotStarted,
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(
            int id,
            UpdateTaskStatusRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == Guid.Parse(userId));

            if (task == null)
                return NotFound("Task not found");

            task.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.UserId == Guid.Parse(userId));

            if (task == null)
                return NotFound("Task not found");

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
