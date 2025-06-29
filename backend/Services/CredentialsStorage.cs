using System.Text.Json;
using TPLinkWebUI.Models;

namespace TPLinkWebUI.Services
{
    public class CredentialsStorage
    {
        private readonly string _filePath = "credentials.json";
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<CredentialsStorage> _logger;

        public CredentialsStorage(IEncryptionService encryptionService, ILogger<CredentialsStorage> logger)
        {
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<LoginRequest?> LoadAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return null;

                var json = await File.ReadAllTextAsync(_filePath);
                var encryptedData = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                
                if (encryptedData != null && 
                    encryptedData.TryGetValue("username", out var encryptedUsername) && 
                    encryptedData.TryGetValue("password", out var encryptedPassword))
                {
                    return new LoginRequest
                    {
                        Username = _encryptionService.Decrypt(encryptedUsername),
                        Password = _encryptionService.Decrypt(encryptedPassword)
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load credentials");
                return null;
            }
        }

        public async Task SaveAsync(LoginRequest credentials)
        {
            try
            {
                var encryptedData = new Dictionary<string, string>
                {
                    { "username", _encryptionService.Encrypt(credentials.Username) },
                    { "password", _encryptionService.Encrypt(credentials.Password) },
                    { "created", DateTime.UtcNow.ToString("O") },
                    { "version", "2.0" }
                };
                
                var json = JsonSerializer.Serialize(encryptedData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_filePath, json);
                
                // Set restrictive file permissions on Unix systems
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    File.SetUnixFileMode(_filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                
                _logger.LogInformation("Encrypted credentials saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save credentials");
                throw new InvalidOperationException("Failed to save credentials", ex);
            }
        }
        
        public void Delete()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    _logger.LogInformation("Credentials deleted successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete credentials");
                throw new InvalidOperationException("Failed to delete credentials", ex);
            }
        }
    }
}