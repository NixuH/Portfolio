using Microsoft.AspNetCore.Mvc;
using TaskManagement.Server.Models;

namespace TaskManagement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var url = _configuration["supabase:Url"];
            var key = _configuration["supabase:PublishableKey"];

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Add("apikey", key);

            var response = await client.PostAsJsonAsync(
                $"{url}/auth/v1/signup",
                request);

            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var url = _configuration["supabase:Url"];
            var key = _configuration["supabase:PublishableKey"];

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Add("apikey", key);

            var response = await client.PostAsJsonAsync(
                $"{url}/auth/v1/token?grant_type=password",
                request);

            var content = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, content);
        }


    }
}
