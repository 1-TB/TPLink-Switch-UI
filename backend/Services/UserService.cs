using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TPLinkWebUI.Data;
using TPLinkWebUI.Models;

namespace TPLinkWebUI.Services;

public class UserService
{
    private readonly SwitchHistoryContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IEncryptionService _encryptionService;

    public UserService(SwitchHistoryContext context, ILogger<UserService> logger, IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    public async Task<bool> IsInitialSetupRequiredAsync()
    {
        return !await _context.Users.AnyAsync();
    }

    public async Task<(bool Success, string Message, User? User)> CreateUserAsync(UserRegistrationRequest request, bool isInitialSetup = false)
    {
        try
        {
            // Check if initial setup is required
            if (!isInitialSetup && await IsInitialSetupRequiredAsync())
            {
                return (false, "Initial setup is required", null);
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return (false, "Username already exists", null);
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return (false, "Email already exists", null);
            }

            var salt = _encryptionService.GenerateSalt();
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordSalt = salt,
                PasswordHash = _encryptionService.HashPassword(request.Password, salt),
                Role = isInitialSetup ? UserRole.Admin : UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {Username} ({Email})", user.Username, user.Email);
            return (true, "User created successfully", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user: {Username}", request.Username);
            return (false, $"Failed to create user: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, string? SessionToken)> AuthenticateAsync(UserLoginRequest request, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null || string.IsNullOrEmpty(user.PasswordSalt) || !VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Authentication failed for user: {Username}", request.Username);
                return (false, "Invalid username or password", null);
            }

            // Create session
            var sessionToken = GenerateSessionToken();
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = sessionToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };

            _context.UserSessions.Add(session);

            // Update user's last login
            user.LastLoginAt = DateTime.UtcNow;
            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User authenticated successfully: {Username}", user.Username);
            return (true, "Authentication successful", sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user: {Username}", request.Username);
            return (false, $"Authentication failed: {ex.Message}", null);
        }
    }

    public async Task<User?> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && 
                                        s.IsActive && 
                                        s.ExpiresAt > DateTime.UtcNow);

            if (session?.User?.IsActive == true)
            {
                return session.User;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session token");
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session != null)
            {
                session.IsActive = false;
                _context.UserSessions.Update(session);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        try
        {
            var expiredSessions = await _context.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }

            if (expiredSessions.Any())
            {
                _context.UserSessions.UpdateRange(expiredSessions);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired sessions");
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }


    private bool VerifyPassword(string password, string hash, string salt)
    {
        return _encryptionService.VerifyPassword(password, hash, salt);
    }

    private string GenerateSessionToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}