using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class PortHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public int PortNumber { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string Status { get; set; } = string.Empty;
        
        public string SpeedConfig { get; set; } = string.Empty;
        
        public string SpeedActual { get; set; } = string.Empty;
        
        public string FlowControlConfig { get; set; } = string.Empty;
        
        public string FlowControlActual { get; set; } = string.Empty;
        
        public string Trunk { get; set; } = string.Empty;
        
        public string ChangeType { get; set; } = string.Empty; // "CONFIG_CHANGE", "STATUS_CHANGE", "PERIODIC_SNAPSHOT"
        
        public string? PreviousValue { get; set; }
        
        public string? NewValue { get; set; }
        
        public string? Notes { get; set; }
        
        // Enhanced tracking
        public int? UserId { get; set; }
        
        public string? Username { get; set; }
        
        public TimeSpan? DowntimeDuration { get; set; }
        
        public DateTime? LastUpTime { get; set; }
        
        public bool IsEnabled => Status == "Enabled";
        
        public bool IsConnected => SpeedActual != "Link Down";
        
        public string GetFormattedDowntime()
        {
            if (!DowntimeDuration.HasValue) return "";
            
            var duration = DowntimeDuration.Value;
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays} days, {duration.Hours} hours";
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} hours, {duration.Minutes} minutes";
            return $"{(int)duration.TotalMinutes} minutes";
        }
        
        public static PortHistoryEntry FromPortInfo(PortInfo portInfo, string changeType, string? previousValue = null, string? newValue = null, string? notes = null, int? userId = null, string? username = null, TimeSpan? downtimeDuration = null, DateTime? lastUpTime = null)
        {
            return new PortHistoryEntry
            {
                PortNumber = portInfo.PortNumber,
                Timestamp = DateTime.UtcNow,
                Status = portInfo.Status,
                SpeedConfig = portInfo.SpeedConfig,
                SpeedActual = portInfo.SpeedActual,
                FlowControlConfig = portInfo.FlowControlConfig,
                FlowControlActual = portInfo.FlowControlActual,
                Trunk = portInfo.Trunk,
                ChangeType = changeType,
                PreviousValue = previousValue,
                NewValue = newValue,
                Notes = notes,
                UserId = userId,
                Username = username,
                DowntimeDuration = downtimeDuration,
                LastUpTime = lastUpTime
            };
        }
    }
}