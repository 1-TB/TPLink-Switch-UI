using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TPLinkWebUI.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
        string HashPassword(string password, string salt);
        string GenerateSalt();
        bool VerifyPassword(string password, string hash, string salt);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly string _encryptionKey;
        private readonly ILogger<EncryptionService> _logger;
        private readonly string _keyFilePath = "encryption.key";

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _logger = logger;
            _encryptionKey = configuration["Encryption:Key"] ?? GetOrCreatePersistentKey();
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32)); // Ensure 32 bytes for AES-256
                aes.Key = key;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                var iv = aes.IV;
                var encryptedBytes = msEncrypt.ToArray();
                var result = new byte[iv.Length + encryptedBytes.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);
                
                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                var fullCipher = Convert.FromBase64String(encryptedText);
                using var aes = Aes.Create();
                var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
                aes.Key = key;

                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipher);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public string HashPassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            if (string.IsNullOrEmpty(salt))
                throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

            // Use PBKDF2 with SHA-256 for password hashing (more secure than SHA-256 alone)
            using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32); // 256 bits
            return Convert.ToBase64String(hash);
        }

        public string GenerateSalt()
        {
            var salt = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;

            try
            {
                var computedHash = HashPassword(password, salt);
                return computedHash.Equals(hash, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify password");
                return false;
            }
        }

        private string GetOrCreatePersistentKey()
        {
            try
            {
                // Try to load existing key
                if (File.Exists(_keyFilePath))
                {
                    var existingKey = File.ReadAllText(_keyFilePath).Trim();
                    if (!string.IsNullOrEmpty(existingKey))
                    {
                        _logger.LogDebug("Using existing encryption key");
                        return existingKey;
                    }
                }

                // Generate new key and save it
                var newKey = GenerateRandomKey();
                File.WriteAllText(_keyFilePath, newKey);
                
                // Set restrictive file permissions on Unix systems
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    File.SetUnixFileMode(_keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                
                _logger.LogInformation("Generated and saved new encryption key");
                return newKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or create persistent encryption key, using in-memory key");
                return GenerateRandomKey();
            }
        }

        private string GenerateRandomKey()
        {
            var key = new byte[32]; // 256 bits for AES-256
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }
    }
}