using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class SwitchConnectivityHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public bool IsReachable { get; set; }
        
        public string? IpAddress { get; set; }
        
        public int? ResponseTimeMs { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public TimeSpan? DowntimeDuration { get; set; }
        
        public string? Notes { get; set; }
        
        public static SwitchConnectivityHistoryEntry CreateReachable(string? ipAddress, int? responseTimeMs = null)
        {
            return new SwitchConnectivityHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                IsReachable = true,
                IpAddress = ipAddress,
                ResponseTimeMs = responseTimeMs,
                Notes = "Switch is reachable"
            };
        }
        
        public static SwitchConnectivityHistoryEntry CreateUnreachable(string? ipAddress, string? errorMessage, TimeSpan? downtimeDuration = null)
        {
            return new SwitchConnectivityHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                IsReachable = false,
                IpAddress = ipAddress,
                ErrorMessage = errorMessage,
                DowntimeDuration = downtimeDuration,
                Notes = downtimeDuration.HasValue 
                    ? $"Switch unreachable for {FormatDuration(downtimeDuration.Value)}"
                    : "Switch unreachable"
            };
        }
        
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays} days, {duration.Hours} hours";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} hours, {duration.Minutes} minutes";
            return $"{(int)duration.TotalMinutes} minutes";
        }
    }
}