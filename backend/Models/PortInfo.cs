namespace TPLinkWebUI.Models
{
    public class PortInfo
    {
        public int PortNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SpeedConfig { get; set; } = string.Empty;
        public string SpeedActual { get; set; } = string.Empty;
        public string FlowControlConfig { get; set; } = string.Empty;
        public string FlowControlActual { get; set; } = string.Empty;
        public string Trunk { get; set; } = string.Empty;
        public bool IsEnabled => Status == "Enabled";
        public bool IsConnected => SpeedActual != "Link Down";
    }

    public class PortConfigRequest
    {
        public int Port { get; set; }
        public bool Enable { get; set; }
        public int Speed { get; set; } = 1; // 1 = Auto
        public bool FlowControl { get; set; } = false;
    }

    public class PortInfoResponse
    {
        public List<PortInfo> Ports { get; set; } = new();
        public int MaxPorts { get; set; }
        public string RawData { get; set; } = string.Empty;

        public static PortInfoResponse Parse(string portInfoText)
        {
            var response = new PortInfoResponse
            {
                RawData = portInfoText
            };

            var lines = portInfoText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var dataStarted = false;

            foreach (var line in lines)
            {
                if (line.Contains("Port | Status"))
                {
                    dataStarted = true;
                    continue;
                }

                if (line.Contains("-----|"))
                {
                    continue;
                }

                if (dataStarted && line.Trim().Length > 0)
                {
                    var parts = line.Split('|', StringSplitOptions.TrimEntries);
                    if (parts.Length >= 6 && int.TryParse(parts[0], out var portNum))
                    {
                        response.Ports.Add(new PortInfo
                        {
                            PortNumber = portNum,
                            Status = parts[1],
                            SpeedConfig = parts[2],
                            SpeedActual = parts[3],
                            FlowControlConfig = parts[4],
                            FlowControlActual = parts[5],
                            Trunk = parts.Length > 6 ? parts[6] : ""
                        });
                    }
                }
            }

            response.MaxPorts = response.Ports.Count > 0 ? response.Ports.Max(p => p.PortNumber) : 24;
            return response;
        }
    }
}