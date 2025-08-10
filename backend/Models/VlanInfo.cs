namespace TPLinkWebUI.Models
{
    public class VlanInfo
    {
        public int VlanId { get; set; }
        public string VlanName { get; set; } = string.Empty;
        public string MemberPorts { get; set; } = string.Empty;
        public List<int> PortNumbers { get; set; } = new();
        public List<int> TaggedPorts { get; set; } = new();
        public List<int> UntaggedPorts { get; set; } = new();
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

            // Try to parse 802.1Q VLAN data from JavaScript data structure
            if (vlanConfigText.Contains("qvlan_ds"))
            {
                return Parse8021QData(vlanConfigText);
            }

            // Fallback to legacy port-based VLAN parsing
            return ParsePortBasedData(vlanConfigText);
        }

        private static VlanConfigResponse Parse8021QData(string vlanConfigText)
        {
            var response = new VlanConfigResponse
            {
                RawData = vlanConfigText
            };

            try
            {
                // Parse JavaScript data structure for 802.1Q VLANs
                var stateMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"state:\s*(\d+)");
                response.IsEnabled = stateMatch.Success && stateMatch.Groups[1].Value == "1";

                var portNumMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"portNum:\s*(\d+)");
                if (portNumMatch.Success && int.TryParse(portNumMatch.Groups[1].Value, out var portNum))
                {
                    response.TotalPorts = portNum;
                }

                var countMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"count:\s*(\d+)");
                if (countMatch.Success && int.TryParse(countMatch.Groups[1].Value, out var count))
                {
                    response.VlanCount = count;
                }

                // Parse VLAN IDs
                var vidsMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"vids:\s*\[([^\]]+)\]");
                var vids = new List<int>();
                if (vidsMatch.Success)
                {
                    var vidStrings = vidsMatch.Groups[1].Value.Split(',');
                    foreach (var vidStr in vidStrings)
                    {
                        if (int.TryParse(vidStr.Trim(), out var vid))
                        {
                            vids.Add(vid);
                        }
                    }
                }

                // Parse VLAN names
                var namesMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"names:\s*\[([^\]]+)\]");
                var names = new List<string>();
                if (namesMatch.Success)
                {
                    var nameStrings = namesMatch.Groups[1].Value.Split(',');
                    foreach (var nameStr in nameStrings)
                    {
                        var cleanName = nameStr.Trim().Trim('\'', '"');
                        names.Add(cleanName);
                    }
                }

                // Parse tagged members (hex values)
                var tagMbrsMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"tagMbrs:\s*\[([^\]]+)\]");
                var taggedMembers = new List<uint>();
                if (tagMbrsMatch.Success)
                {
                    var tagStrings = tagMbrsMatch.Groups[1].Value.Split(',');
                    foreach (var tagStr in tagStrings)
                    {
                        var cleanTag = tagStr.Trim();
                        if (cleanTag.StartsWith("0x") || cleanTag.StartsWith("0X"))
                        {
                            if (uint.TryParse(cleanTag[2..], System.Globalization.NumberStyles.HexNumber, null, out var tagValue))
                            {
                                taggedMembers.Add(tagValue);
                            }
                        }
                        else if (uint.TryParse(cleanTag, out var tagValue))
                        {
                            taggedMembers.Add(tagValue);
                        }
                    }
                }

                // Parse untagged members (hex values)
                var untagMbrsMatch = System.Text.RegularExpressions.Regex.Match(vlanConfigText, @"untagMbrs:\s*\[([^\]]+)\]");
                var untaggedMembers = new List<uint>();
                if (untagMbrsMatch.Success)
                {
                    var untagStrings = untagMbrsMatch.Groups[1].Value.Split(',');
                    foreach (var untagStr in untagStrings)
                    {
                        var cleanUntag = untagStr.Trim();
                        if (cleanUntag.StartsWith("0x") || cleanUntag.StartsWith("0X"))
                        {
                            if (uint.TryParse(cleanUntag[2..], System.Globalization.NumberStyles.HexNumber, null, out var untagValue))
                            {
                                untaggedMembers.Add(untagValue);
                            }
                        }
                        else if (uint.TryParse(cleanUntag, out var untagValue))
                        {
                            untaggedMembers.Add(untagValue);
                        }
                    }
                }

                // Build VLAN info objects
                for (int i = 0; i < vids.Count && i < names.Count; i++)
                {
                    var vlanInfo = new VlanInfo
                    {
                        VlanId = vids[i],
                        VlanName = i < names.Count ? names[i] : $"VLAN{vids[i]}"
                    };

                    // Parse tagged ports from bitmask
                    var taggedPorts = new List<int>();
                    var untaggedPorts = new List<int>();

                    if (i < taggedMembers.Count)
                    {
                        taggedPorts = ParsePortsFromBitmask(taggedMembers[i], response.TotalPorts);
                    }

                    if (i < untaggedMembers.Count)
                    {
                        untaggedPorts = ParsePortsFromBitmask(untaggedMembers[i], response.TotalPorts);
                    }

                    vlanInfo.TaggedPorts = taggedPorts;
                    vlanInfo.UntaggedPorts = untaggedPorts;
                    vlanInfo.PortNumbers = taggedPorts.Concat(untaggedPorts).Distinct().OrderBy(p => p).ToList();
                    vlanInfo.MemberPorts = string.Join(",", vlanInfo.PortNumbers);

                    response.Vlans.Add(vlanInfo);
                }
            }
            catch (Exception)
            {
                // If parsing fails, return basic response
                response.IsEnabled = false;
                response.TotalPorts = 24; // Default assumption
                response.VlanCount = 0;
            }

            return response;
        }

        private static VlanConfigResponse ParsePortBasedData(string vlanConfigText)
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
                            VlanName = $"VLAN{vlanId}",
                            MemberPorts = memberPorts,
                            PortNumbers = portNumbers,
                            UntaggedPorts = portNumbers, // Port-based VLANs are untagged
                            TaggedPorts = new List<int>()
                        });
                    }
                }
            }

            return response;
        }

        private static List<int> ParsePortsFromBitmask(uint bitmask, int totalPorts)
        {
            var ports = new List<int>();
            for (int i = 0; i < totalPorts && i < 32; i++)
            {
                if ((bitmask & (1u << i)) != 0)
                {
                    ports.Add(i + 1); // Port numbers are 1-based
                }
            }
            return ports;
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
        public string VlanName { get; set; } = string.Empty;
        public int[] TaggedPorts { get; set; } = Array.Empty<int>();
        public int[] UntaggedPorts { get; set; } = Array.Empty<int>();
        
        // Legacy support for backwards compatibility
        public int[] Ports { get; set; } = Array.Empty<int>();
    }

    public class SetPvidRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
        public int Pvid { get; set; }
    }

    public class DeleteVlanRequest
    {
        public int[] VlanIds { get; set; } = Array.Empty<int>();
    }
}