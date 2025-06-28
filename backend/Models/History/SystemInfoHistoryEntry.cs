using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class SystemInfoHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string DeviceName { get; set; } = string.Empty;
        
        public string MacAddress { get; set; } = string.Empty;
        
        public string IpAddress { get; set; } = string.Empty;
        
        public string SubnetMask { get; set; } = string.Empty;
        
        public string Gateway { get; set; } = string.Empty;
        
        public string FirmwareVersion { get; set; } = string.Empty;
        
        public string HardwareVersion { get; set; } = string.Empty;
        
        public string SystemUptime { get; set; } = string.Empty;
        
        public string ChangeType { get; set; } = string.Empty; // "CONFIG_CHANGE", "FIRMWARE_UPDATE", "PERIODIC_SNAPSHOT"
        
        public string? Notes { get; set; }
        
        public static SystemInfoHistoryEntry FromSystemInfoResponse(SystemInfoResponse systemInfo, string changeType, string? notes = null)
        {
            return new SystemInfoHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                DeviceName = systemInfo.DeviceName,
                MacAddress = systemInfo.MacAddress,
                IpAddress = systemInfo.IpAddress,
                SubnetMask = systemInfo.SubnetMask,
                Gateway = systemInfo.Gateway,
                FirmwareVersion = systemInfo.FirmwareVersion,
                HardwareVersion = systemInfo.HardwareVersion,
                SystemUptime = systemInfo.SystemUptime,
                ChangeType = changeType,
                Notes = notes
            };
        }
    }
}