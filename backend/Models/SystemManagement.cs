namespace TPLinkWebUI.Models
{
    // System Name Setting
    public class SystemNameRequest
    {
        public string SystemName { get; set; } = string.Empty;
    }

    // IP Configuration
    public class IpConfigRequest
    {
        public bool DhcpEnabled { get; set; }
        public string? IpAddress { get; set; }
        public string? SubnetMask { get; set; }
        public string? Gateway { get; set; }
    }

    // LED Control
    public class LedControlRequest
    {
        public bool LedEnabled { get; set; }
    }

    // User Account Management
    public class UserAccountRequest
    {
        public string NewUsername { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Save/Reset Operations
    public class SaveConfigRequest
    {
        public bool SaveBeforeOperation { get; set; } = true;
    }

    public class RebootRequest
    {
        public bool SaveConfig { get; set; } = true;
    }

    // Port Statistics
    public class ClearPortStatsRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
    }
}