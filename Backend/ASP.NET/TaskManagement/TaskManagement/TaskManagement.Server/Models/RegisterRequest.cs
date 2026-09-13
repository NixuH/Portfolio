using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Server.Models
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter and one digit."
        )]
        public string Password { get; set; } = string.Empty;
    }
}
