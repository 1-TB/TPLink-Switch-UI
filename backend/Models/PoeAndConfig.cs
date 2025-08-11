namespace TPLinkWebUI.Models
{
    // PoE Global Configuration
    public class PoeGlobalConfigRequest
    {
        public float PowerLimit { get; set; } // Watts
    }

    // PoE Port Configuration
    public class PoePortConfigRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
        public PoePortState State { get; set; } = PoePortState.NoChange;
        public PoePriority Priority { get; set; } = PoePriority.NoChange;
        public PoePowerLimit PowerLimit { get; set; } = PoePowerLimit.NoChange;
        public float? ManualPowerLimit { get; set; } // 0.1-30.0W for manual setting
    }

    public enum PoePortState
    {
        Disable = 1,
        Enable = 2,
        NoChange = 7
    }

    public enum PoePriority
    {
        High = 1,
        Middle = 2,
        Low = 3,
        NoChange = 7
    }

    public enum PoePowerLimit
    {
        Auto = 1,
        Class1 = 2,    // 4.0W
        Class2 = 3,    // 7.0W
        Class3 = 4,    // 15.4W
        Class4 = 5,    // 30.0W
        Manual = 6,
        NoChange = 7
    }

    // Configuration Management
    public class ConfigBackupRequest
    {
        // Configuration backup doesn't typically need parameters
        // Response will be a file download
    }

    public class ConfigRestoreRequest
    {
        public IFormFile ConfigFile { get; set; } = null!;
    }

    public class FirmwareUpgradeRequest
    {
        public IFormFile FirmwareFile { get; set; } = null!;
    }
}