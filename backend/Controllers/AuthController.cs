using Microsoft.AspNetCore.Mvc;
using TPLinkWebUI.Models;
using TPLinkWebUI.Services;

namespace TPLinkWebUI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly SwitchService _switchService;
    private readonly CredentialsStorage _credentialsStorage;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserService userService,
        SwitchService switchService,
        CredentialsStorage credentialsStorage,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _switchService = switchService;
        _credentialsStorage = credentialsStorage;
        _logger = logger;
    }

    [HttpGet("setup/required")]
    public async Task<IActionResult> IsSetupRequired()
    {
        try
        {
            var setupRequired = await _userService.IsInitialSetupRequiredAsync();
            return Ok(new { setupRequired });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is required");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("setup")]
    public async Task<IActionResult> InitialSetup([FromBody] InitialSetupRequest request)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Initial setup attempt from {ClientIP}", clientIp);

            // Check if setup is actually required
            if (!await _userService.IsInitialSetupRequiredAsync())
            {
                return BadRequest(new { success = false, message = "Setup has already been completed" });
            }

            // Test switch connection first
            try
            {
                var loginRequest = new LoginRequest
                {
                    Host = request.SwitchHost,
                    Username = request.SwitchUsername,
                    Password = request.SwitchPassword
                };

                await _switchService.EnsureClientAsync(loginRequest);
                _logger.LogInformation("Switch connection test successful during setup");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Switch connection failed during setup");
                return BadRequest(new { success = false, message = $"Switch connection failed: {ex.Message}" });
            }

            // Create admin user
            var userResult = await _userService.CreateUserAsync(request.UserAccount, isInitialSetup: true);
            if (!userResult.Success)
            {
                return BadRequest(new { success = false, message = userResult.Message });
            }

            // Store switch credentials
            var switchCredentials = new LoginRequest
            {
                Host = request.SwitchHost,
                Username = request.SwitchUsername,
                Password = request.SwitchPassword
            };
            await _credentialsStorage.SaveAsync(switchCredentials);

            _logger.LogInformation("Initial setup completed successfully for user {Username}", request.UserAccount.Username);

            return Ok(new { 
                success = true, 
                message = "Initial setup completed successfully",
                user = new {
                    id = userResult.User!.Id,
                    username = userResult.User.Username,
                    email = userResult.User.Email,
                    role = userResult.User.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial setup");
            return BadRequest(new { success = false, message = $"Setup failed: {ex.Message}" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

            _logger.LogInformation("User login attempt: {Username} from {ClientIP}", request.Username, clientIp);

            var result = await _userService.AuthenticateAsync(request, clientIp, userAgent);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            // Set session cookie
            Response.Cookies.Append("session_token", result.SessionToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            return Ok(new { 
                success = true, 
                message = "Login successful",
                sessionToken = result.SessionToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return BadRequest(new { success = false, message = $"Login failed: {ex.Message}" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionToken = Request.Cookies["session_token"] ?? 
                              Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(sessionToken))
            {
                await _userService.LogoutAsync(sessionToken);
            }

            Response.Cookies.Delete("session_token");

            return Ok(new { success = true, message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var sessionToken = Request.Cookies["session_token"] ?? 
                              Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized(new { success = false, message = "No session token provided" });
            }

            var user = await _userService.ValidateSessionAsync(sessionToken);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Invalid or expired session" });
            }

            return Ok(new {
                success = true,
                user = new {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role.ToString(),
                    lastLoginAt = user.LastLoginAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationRequest request)
    {
        try
        {
            // Only allow registration if initial setup is complete
            if (await _userService.IsInitialSetupRequiredAsync())
            {
                return BadRequest(new { success = false, message = "Initial setup must be completed first" });
            }

            // Authorization check - only admins should be able to create new users
            var sessionToken = Request.Cookies["session_token"] ?? 
                              Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(sessionToken))
            {
                return Unauthorized(new { success = false, message = "Authentication required" });
            }

            var currentUser = await _userService.ValidateSessionAsync(sessionToken);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "Invalid or expired session" });
            }

            if (currentUser.Role != UserRole.Admin)
            {
                return Forbid("Only administrators can create new users");
            }

            var result = await _userService.CreateUserAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("User {NewUsername} created by admin {AdminUsername}", 
                request.Username, currentUser.Username);

            return Ok(new { 
                success = true, 
                message = "User registered successfully",
                user = new {
                    id = result.User!.Id,
                    username = result.User.Username,
                    email = result.User.Email,
                    role = result.User.Role.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return BadRequest(new { success = false, message = $"Registration failed: {ex.Message}" });
        }
    }
}