using System.Text.Json;
using TPLinkWebUI.Models;

namespace TPLinkWebUI.Services
{
    public class CredentialsStorage
    {
        private readonly string _filePath = "credentials.json";

        public async Task<LoginRequest?> LoadAsync()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return null;

                var json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<LoginRequest>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveAsync(LoginRequest credentials)
        {
            try
            {
                var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch
            {
                // Handle save errors as needed
            }
        }
    }
}