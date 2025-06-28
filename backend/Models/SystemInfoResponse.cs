namespace TPLinkWebUI.Models
{
    public class SystemInfoResponse
    {
        public string DeviceName { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string SubnetMask { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string HardwareVersion { get; set; } = string.Empty;
        public string SystemUptime { get; set; } = string.Empty;

        public static SystemInfoResponse Parse(string systemInfoText)
        {
            var response = new SystemInfoResponse();
            
            // Parse the formatted string returned by TplinkClient.GetSystemInfoAsync()
            var lines = systemInfoText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    switch (key)
                    {
                        case "Device Description":
                            response.DeviceName = value;
                            break;
                        case "MAC Address":
                            response.MacAddress = value;
                            break;
                        case "IP Address":
                            response.IpAddress = value;
                            break;
                        case "Subnet Mask":
                            response.SubnetMask = value;
                            break;
                        case "Gateway":
                            response.Gateway = value;
                            break;
                        case "Firmware Version":
                            response.FirmwareVersion = value;
                            break;
                        case "Hardware Version":
                            response.HardwareVersion = value;
                            break;
                    }
                }
            }
            
            // Calculate uptime (this would need to be implemented based on switch capabilities)
            response.SystemUptime = "Not available";
            
            return response;
        }
    }
}