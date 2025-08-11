using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class UserActivityHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public int? UserId { get; set; }
        
        public string Username { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public string ActionType { get; set; } = string.Empty; // "CONFIG_CHANGE", "LOGIN", "LOGOUT", "SYSTEM_COMMAND"
        
        public string Description { get; set; } = string.Empty;
        
        public string? TargetEntity { get; set; } // e.g., "Port 8", "VLAN 10", "System"
        
        public string? PreviousValue { get; set; }
        
        public string? NewValue { get; set; }
        
        public string? IpAddress { get; set; }
        
        public string? UserAgent { get; set; }
        
        public bool IsSuccess { get; set; } = true;
        
        public string? ErrorMessage { get; set; }
        
        public static UserActivityHistoryEntry CreateConfigChange(
            int? userId, 
            string username, 
            string description, 
            string? targetEntity = null, 
            string? previousValue = null, 
            string? newValue = null,
            string? ipAddress = null)
        {
            return new UserActivityHistoryEntry
            {
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow,
                ActionType = "CONFIG_CHANGE",
                Description = description,
                TargetEntity = targetEntity,
                PreviousValue = previousValue,
                NewValue = newValue,
                IpAddress = ipAddress,
                IsSuccess = true
            };
        }
        
        public static UserActivityHistoryEntry CreateLogin(int? userId, string username, string? ipAddress = null, string? userAgent = null)
        {
            return new UserActivityHistoryEntry
            {
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow,
                ActionType = "LOGIN",
                Description = "User logged in",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = true
            };
        }
        
        public static UserActivityHistoryEntry CreateLogout(int? userId, string username, string? ipAddress = null)
        {
            return new UserActivityHistoryEntry
            {
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow,
                ActionType = "LOGOUT",
                Description = "User logged out",
                IpAddress = ipAddress,
                IsSuccess = true
            };
        }
        
        public static UserActivityHistoryEntry CreateSystemCommand(
            int? userId, 
            string username, 
            string description, 
            bool isSuccess = true, 
            string? errorMessage = null,
            string? ipAddress = null)
        {
            return new UserActivityHistoryEntry
            {
                UserId = userId,
                Username = username,
                Timestamp = DateTime.UtcNow,
                ActionType = "SYSTEM_COMMAND",
                Description = description,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                IpAddress = ipAddress
            };
        }
    }
}