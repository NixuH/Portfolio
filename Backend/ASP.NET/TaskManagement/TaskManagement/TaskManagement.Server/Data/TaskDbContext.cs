using Microsoft.EntityFrameworkCore;
using TaskManagement.Server.Models;

namespace TaskManagement.Server.Data
{
    public class TaskDbContext : DbContext
    {
        public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
    }
}
