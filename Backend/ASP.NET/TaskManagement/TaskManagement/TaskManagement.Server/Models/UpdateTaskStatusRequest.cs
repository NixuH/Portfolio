using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Server.Models
{
    public class UpdateTaskStatusRequest
    {
        public TaskStatus Status { get; set; }
    }
}