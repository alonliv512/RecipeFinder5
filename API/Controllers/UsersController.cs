using Microsoft.AspNetCore.Mvc;
using DAL;
using Model;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserDB _userDB;

        public UsersController(UserDB userDB)
        {
            _userDB = userDB;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userDB.GetAllUsersAsync();

                // איפוס סיסמה עבור כל משתמש ברשימה משיקולי אבטחה
                foreach (var user in users)
                {
                    user.PasswordHash = string.Empty;
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userDB.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found.");
                }

                // איפוס הסיסמה לפני החזרה ללקוח
                user.PasswordHash = string.Empty;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/users/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            try
            {
                if (await _userDB.UsernameExistsAsync(user.Username))
                {
                    return BadRequest("Username already exists.");
                }

                int newId = await _userDB.AddUserAsync(user);
                return Ok(new { UserID = newId, Message = "User registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/users/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _userDB.CheckLoginAsync(loginDto.Username, loginDto.Password);

                if (user == null)
                {
                    return Unauthorized("שם משתמש או סיסמה שגויים.");
                }

                // איפוס הגיבוב לפני שליחתו בדפדפן
                user.PasswordHash = string.Empty;

                return Ok(new { Message = "התחברות בוצעה בהצלחה!", User = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

// מחלקת ה-DTO להתחברות
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
