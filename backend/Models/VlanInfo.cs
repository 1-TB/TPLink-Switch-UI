namespace TPLinkWebUI.Models
{
    public class VlanInfo
    {
        public int VlanId { get; set; }
        public string MemberPorts { get; set; } = string.Empty;
        public List<int> PortNumbers { get; set; } = new();
    }

    public class VlanConfigResponse
    {
        public bool IsEnabled { get; set; }
        public int TotalPorts { get; set; }
        public int VlanCount { get; set; }
        public List<VlanInfo> Vlans { get; set; } = new();
        public string RawData { get; set; } = string.Empty;

        public static VlanConfigResponse Parse(string vlanConfigText)
        {
            var response = new VlanConfigResponse
            {
                RawData = vlanConfigText
            };

            var lines = vlanConfigText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var dataStarted = false;

            foreach (var line in lines)
            {
                if (line.Contains("Status:"))
                {
                    response.IsEnabled = line.Contains("Enabled");
                }
                else if (line.Contains("Total Ports:"))
                {
                    var parts = line.Split(':', StringSplitOptions.TrimEntries);
                    if (parts.Length == 2 && int.TryParse(parts[1], out var totalPorts))
                    {
                        response.TotalPorts = totalPorts;
                    }
                }
                else if (line.Contains("Number of VLANs:"))
                {
                    var parts = line.Split(':', StringSplitOptions.TrimEntries);
                    if (parts.Length == 2 && int.TryParse(parts[1], out var vlanCount))
                    {
                        response.VlanCount = vlanCount;
                    }
                }
                else if (line.Contains("VLAN ID | Member Ports"))
                {
                    dataStarted = true;
                    continue;
                }
                else if (line.Contains("--------|"))
                {
                    continue;
                }
                else if (dataStarted && line.Trim().Length > 0 && !line.Contains("No VLANs"))
                {
                    var parts = line.Split('|', StringSplitOptions.TrimEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[0], out var vlanId))
                    {
                        var memberPorts = parts[1];
                        var portNumbers = ParsePortNumbers(memberPorts);
                        
                        response.Vlans.Add(new VlanInfo
                        {
                            VlanId = vlanId,
                            MemberPorts = memberPorts,
                            PortNumbers = portNumbers
                        });
                    }
                }
            }

            return response;
        }

        private static List<int> ParsePortNumbers(string portRange)
        {
            var ports = new List<int>();
            if (string.IsNullOrWhiteSpace(portRange)) return ports;

            var ranges = portRange.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var range in ranges)
            {
                var trimmed = range.Trim();
                if (trimmed.Contains('-'))
                {
                    var rangeParts = trimmed.Split('-');
                    if (rangeParts.Length == 2 && 
                        int.TryParse(rangeParts[0], out var start) && 
                        int.TryParse(rangeParts[1], out var end))
                    {
                        for (int i = start; i <= end; i++)
                        {
                            ports.Add(i);
                        }
                    }
                }
                else if (int.TryParse(trimmed, out var port))
                {
                    ports.Add(port);
                }
            }

            return ports.Distinct().OrderBy(p => p).ToList();
        }
    }

    public class CreateVlanRequest
    {
        public int VlanId { get; set; }
        public int[] Ports { get; set; } = Array.Empty<int>();
    }

    public class DeleteVlanRequest
    {
        public int[] VlanIds { get; set; } = Array.Empty<int>();
    }
}