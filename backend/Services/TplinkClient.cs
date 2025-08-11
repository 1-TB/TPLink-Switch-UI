using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Text;

namespace TPLinkWebUI.Services
{
    public class TplinkClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly CookieContainer _cookieContainer;

        public TplinkClient(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true
            };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        }

        public void SetSessionCookie(string cookieValue)
        {
            var uri = new Uri(_baseUrl);
            var cookie = new Cookie("SessionID", cookieValue, "/", uri.Host);
            _cookieContainer.Add(cookie);
        }

        public string? GetSessionCookie()
        {
            var uri = new Uri(_baseUrl);
            var cookies = _cookieContainer.GetCookies(uri);
            return cookies["SessionID"]?.Value;
        }

        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var resp = await _http.GetAsync(_baseUrl + "/SystemInfoRpm.htm", cts.Token);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var resp = await _http.GetAsync(_baseUrl + "/", cts.Token);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task LoginAsync()
        {
            try
            {
                // First test basic connectivity
                if (!await TestConnectionAsync())
                {
                    throw new Exception($"Cannot connect to switch at {_baseUrl}. Please check the IP address and network connectivity.");
                }

                var resp = await _http.GetAsync(_baseUrl + "/");
                var html = await resp.Content.ReadAsStringAsync();
                
                if (html.Contains("logon.cgi"))
                {
                    var form = new Dictionary<string, string>
                    {
                        ["username"] = _username,
                        ["password"] = _password,
                        ["logon"] = "Login"
                    };
                    
                    var login = await _http.PostAsync(_baseUrl + "/logon.cgi", new FormUrlEncodedContent(form));
                    if (!login.IsSuccessStatusCode)
                    {
                        var test = await _http.GetAsync(_baseUrl + "/SystemInfoRpm.htm");
                        if (!test.IsSuccessStatusCode)
                            throw new Exception("Login failed - please check username and password");
                    }
                }
                
                // Verify we can access system info after login
                if (!await IsLoggedInAsync())
                {
                    throw new Exception("Login appeared successful but cannot access system information");
                }
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Connection timeout - switch may be unreachable or slow to respond");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error: {ex.Message}");
            }
        }

        public async Task<string> GetSystemInfoAsync()
        {
            var resp = await _http.GetAsync(_baseUrl + "/SystemInfoRpm.htm");
            resp.EnsureSuccessStatusCode();
            var html = await resp.Content.ReadAsStringAsync();

            // Array-style parsing
            var matchArray = Regex.Match(html, @"var\s+info_ds\s*=\s*new Array\(([^)]*)\);");
            if (matchArray.Success)
            {
                var parts = matchArray.Groups[1].Value.Split(',');
                if (parts.Length >= 7)
                {
                    return $"Device Description: {parts[0].Trim(' ', '"')}\n" +
                           $"MAC Address: {parts[1].Trim(' ', '"')}\n" +
                           $"IP Address: {parts[2].Trim(' ', '"')}\n" +
                           $"Subnet Mask: {parts[3].Trim(' ', '"')}\n" +
                           $"Gateway: {parts[4].Trim(' ', '"')}\n" +
                           $"Firmware Version: {parts[5].Trim(' ', '"')}\n" +
                           $"Hardware Version: {parts[6].Trim(' ', '"')}";
                }
            }

            // Object-literal parsing
            var infoMatch = Regex.Match(html, @"var\s+info_ds\s*=\s*\{([\s\S]*?)\};");
            if (infoMatch.Success)
            {
                var content = infoMatch.Groups[1].Value;
                var dict = new Dictionary<string, string>();
                foreach (var key in new[] { "descriStr", "macStr", "ipStr", "netmaskStr", "gatewayStr", "firmwareStr", "hardwareStr" })
                {
                    var pattern = $@"{key}\s*:\s*\[\s*""([^""]+)""\s*\]";
                    var m = Regex.Match(content, pattern);
                    if (m.Success) dict[key] = m.Groups[1].Value;
                }
                if (dict.Count > 0)
                {
                    return $"Device Description: {dict.GetValueOrDefault("descriStr")}\n" +
                           $"MAC Address: {dict.GetValueOrDefault("macStr")}\n" +
                           $"IP Address: {dict.GetValueOrDefault("ipStr")}\n" +
                           $"Subnet Mask: {dict.GetValueOrDefault("netmaskStr")}\n" +
                           $"Gateway: {dict.GetValueOrDefault("gatewayStr")}\n" +
                           $"Firmware Version: {dict.GetValueOrDefault("firmwareStr")}\n" +
                           $"Hardware Version: {dict.GetValueOrDefault("hardwareStr")}";
                }
            }

            // Fallback: raw HTML
            return html;
        }

        public async Task<string> GetPortInfoAsync()
        {
            var resp = await _http.GetAsync(_baseUrl + "/PortSettingRpm.htm");
            resp.EnsureSuccessStatusCode();
            var html = await resp.Content.ReadAsStringAsync();

            // Extract the all_info JavaScript object
            var allInfoMatch = Regex.Match(html, @"var all_info = \{([\s\S]*?)\};", RegexOptions.Multiline);
            if (!allInfoMatch.Success)
            {
                return "Could not parse port information from response.";
            }

            var content = allInfoMatch.Groups[1].Value;
            
            // Parse arrays from the all_info object
            var stateArray = ParseJsArray(content, "state");
            var spdCfgArray = ParseJsArray(content, "spd_cfg");
            var spdActArray = ParseJsArray(content, "spd_act");
            var fcCfgArray = ParseJsArray(content, "fc_cfg");
            var fcActArray = ParseJsArray(content, "fc_act");
            var trunkArray = ParseJsArray(content, "trunk_info");

            // Get max port number
            var maxPortMatch = Regex.Match(html, @"var max_port_num = (\d+);");
            var maxPorts = maxPortMatch.Success ? int.Parse(maxPortMatch.Groups[1].Value) : 24;

            var result = "=== Port Information ===\n";
            result += "Port | Status  | Speed Config | Speed Actual | Flow Ctrl Cfg | Flow Ctrl Act | Trunk\n";
            result += "-----|---------|--------------|--------------|---------------|---------------|-------\n";

            var speedLabels = new[] { "Link Down", "Auto", "10MH", "10MF", "100MH", "100MF", "1000MF", "" };
            var stateLabels = new[] { "Disabled", "Enabled" };
            var flowLabels = new[] { "Off", "On" };
            var trunkLabels = new[] { "", " (LAG1)", " (LAG2)", " (LAG3)", " (LAG4)", " (LAG5)", " (LAG6)", " (LAG7)", " (LAG8)" };

            for (int i = 0; i < maxPorts; i++)
            {
                var portNum = i + 1;
                var state = i < stateArray.Length ? stateLabels[Math.Min(stateArray[i], stateLabels.Length - 1)] : "Unknown";
                var spdCfg = i < spdCfgArray.Length && spdCfgArray[i] < speedLabels.Length ? speedLabels[spdCfgArray[i]] : "Unknown";
                var spdAct = i < spdActArray.Length && spdActArray[i] < speedLabels.Length ? speedLabels[spdActArray[i]] : "Unknown";
                var fcCfg = i < fcCfgArray.Length ? flowLabels[Math.Min(fcCfgArray[i], flowLabels.Length - 1)] : "Unknown";
                var fcAct = i < fcActArray.Length ? flowLabels[Math.Min(fcActArray[i], flowLabels.Length - 1)] : "Unknown";
                var trunk = i < trunkArray.Length && trunkArray[i] < trunkLabels.Length ? trunkLabels[trunkArray[i]] : "";

                result += $"{portNum,4} | {state,-7} | {spdCfg,-12} | {spdAct,-12} | {fcCfg,-13} | {fcAct,-13} | {trunk}\n";
            }

            return result;
        }

        private int[] ParseJsArray(string content, string arrayName)
        {
            var pattern = $@"{arrayName}:\s*\[([\d,\s]*)\]";
            var match = Regex.Match(content, pattern);
            if (!match.Success) return new int[0];
            
            var arrayContent = match.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(arrayContent)) return new int[0];
            
            return Array.ConvertAll(arrayContent.Split(','), s => int.TryParse(s.Trim(), out var n) ? n : 0);
        }

        public async Task<string> GetVlanConfigurationAsync()
        {
            try
            {
                // Use 802.1Q VLAN endpoint instead of port-based VLAN
                var response = await _http.GetStringAsync(_baseUrl + "/Vlan8021QRpm.htm");
                
                // Return the raw response for parsing by VlanConfigResponse.Parse
                return response;
            }
            catch (Exception ex)
            {
                return $"Error retrieving VLAN configuration: {ex.Message}";
            }
        }

        private VlanConfigData ParseVlanData(string html)
        {
            var data = new VlanConfigData();
            
            try
            {
                // Extract the JavaScript object using more precise regex patterns
                var stateMatch = Regex.Match(html, @"state\s*:\s*(\d+)");
                data.State = stateMatch.Success && stateMatch.Groups[1].Value == "1";
                
                var portNumMatch = Regex.Match(html, @"portNum\s*:\s*(\d+)");
                if (portNumMatch.Success)
                    data.PortNum = int.Parse(portNumMatch.Groups[1].Value);
                
                var countMatch = Regex.Match(html, @"count\s*:\s*(\d+)");
                if (countMatch.Success)
                    data.Count = int.Parse(countMatch.Groups[1].Value);
                
                // Parse VLAN IDs array
                var vidsMatch = Regex.Match(html, @"vids\s*:\s*\[\s*([^\]]+)\s*\]", RegexOptions.Singleline);
                if (vidsMatch.Success)
                {
                    var vidsContent = vidsMatch.Groups[1].Value;
                    data.VlanIds = vidsContent
                        .Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => int.Parse(s.Trim()))
                        .ToArray();
                }
                else
                {
                    data.VlanIds = Array.Empty<int>();
                }
                
                // Parse member bitmasks
                var mbrsMatch = Regex.Match(html, @"mbrs\s*:\s*\[\s*([^\]]+)\s*\]", RegexOptions.Singleline);
                if (mbrsMatch.Success)
                {
                    var mbrsContent = mbrsMatch.Groups[1].Value;
                    data.Members = mbrsContent
                        .Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => 
                        {
                            var trimmed = s.Trim();
                            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                                return Convert.ToInt32(trimmed, 16);
                            else
                                return int.Parse(trimmed);
                        })
                        .ToArray();
                }
                else
                {
                    data.Members = Array.Empty<int>();
                }
                
                // Ensure arrays have consistent lengths
                var minLength = Math.Min(data.VlanIds.Length, data.Members.Length);
                if (data.VlanIds.Length != data.Members.Length)
                {
                    data.VlanIds = data.VlanIds.Take(minLength).ToArray();
                    data.Members = data.Members.Take(minLength).ToArray();
                }
                
                // Update count to match actual data
                data.Count = data.VlanIds.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse VLAN data - {ex.Message}");
                data = new VlanConfigData();
            }
            
            return data;
        }

        private string ConvertBitmaskToPortRange(int bitmask)
        {
            var result = "";
            var mask = 1;
            
            for (int port = 1; port <= 32; port++)
            {
                if ((bitmask & mask) != 0)
                {
                    var startPort = port;
                    var endPort = port;
                    
                    // Find consecutive ports
                    mask <<= 1;
                    while ((bitmask & mask) != 0 && ++port <= 32)
                    {
                        endPort = port;
                        mask <<= 1;
                    }
                    
                    // Add to result
                    if (startPort == endPort)
                    {
                        result += startPort.ToString();
                    }
                    else
                    {
                        result += $"{startPort}-{endPort}";
                    }
                    
                    if (port < 32)
                        result += ",";
                    
                    continue;
                }
                mask <<= 1;
            }
            
            return result.TrimEnd(',');
        }

        public async Task SetPortConfigAsync(int port, bool enable, int speed = 1, bool flowControl = false)
        {
            if (port < 1 || port > 48)
                throw new ArgumentException($"Invalid port number: {port}. Must be between 1 and 48.");

            var form = new Dictionary<string, string>
            {
                ["portid"] = port.ToString() + "^",
                ["state"] = enable ? "1^" : "0^",
                ["speed"] = speed.ToString() + "^",
                ["flowcontrol"] = flowControl ? "1^" : "0^",
                ["apply"] = "Apply"
            };

            var resp = await _http.PostAsync(_baseUrl + "/port_setting.cgi", new FormUrlEncodedContent(form));
            if (!resp.IsSuccessStatusCode)
            {
                var responseContent = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to configure port {port}: HTTP {resp.StatusCode} - {responseContent}");
            }
        }

        public async Task<CableDiagnosticResult> RunCableDiagnosticsAsync(int[] ports)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.");
        
            if (ports.Any(p => p < 1 || p > 48))
                throw new ArgumentException($"Invalid port number(s): {string.Join(",", ports.Where(p => p < 1 || p > 48))}");

            // Build the query string parameters
            var queryParams = new List<string>();
            foreach (var port in ports)
            {
                queryParams.Add($"chk_{port}={port}^");
            }
            queryParams.Add("Apply=Apply");

            var queryString = string.Join("&", queryParams);
            var url = $"{_baseUrl}/cable_diag_get.cgi?{queryString}";

            var resp = await _http.GetAsync(url);
        
            if (!resp.IsSuccessStatusCode)
            {
                var responseContent = await resp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HTTP {resp.StatusCode}: {resp.ReasonPhrase} - {responseContent}");
            }

            var html = await resp.Content.ReadAsStringAsync();
            return ParseCableDiagnosticResults(html);
        }

        private CableDiagnosticResult ParseCableDiagnosticResults(string html)
        {
            var result = new CableDiagnosticResult();

            // Extract cable state array
            var stateMatch = Regex.Match(html, @"var cablestate=\[([^\]]+)\];");
            if (stateMatch.Success)
            {
                var stateValues = stateMatch.Groups[1].Value.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var val) ? val : -1)
                    .ToArray();
                result.CableStates = stateValues;
            }

            // Extract cable length array
            var lengthMatch = Regex.Match(html, @"var cablelength=\[([^\]]+)\];");
            if (lengthMatch.Success)
            {
                var lengthValues = lengthMatch.Groups[1].Value.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var val) ? val : -1)
                    .ToArray();
                result.CableLengths = lengthValues;
            }

            // Extract max port count
            var maxPortMatch = Regex.Match(html, @"var maxPort=(\d+);");
            if (maxPortMatch.Success)
            {
                result.MaxPorts = int.Parse(maxPortMatch.Groups[1].Value);
            }

            return result;
        }

        public async Task CreatePortBasedVlanAsync(int vid, int[] ports)
        {
            // For backwards compatibility, treat all ports as untagged
            await Create8021QVlanAsync(vid, string.Empty, Array.Empty<int>(), ports);
        }

        public async Task Create8021QVlanAsync(int vid, string vlanName, int[] taggedPorts, int[] untaggedPorts)
        {
            try
            {
                // Validate inputs
                if (vid < 1 || vid > 4094)
                    throw new ArgumentException($"Invalid VLAN ID: {vid}. Must be between 1 and 4094.");
                
                var allPorts = (taggedPorts ?? Array.Empty<int>()).Concat(untaggedPorts ?? Array.Empty<int>()).ToArray();
                if (allPorts.Length == 0)
                    throw new ArgumentException("At least one port must be specified.");
                
                if (allPorts.Any(p => p < 1 || p > 48))
                    throw new ArgumentException($"Invalid port number(s): {string.Join(",", allPorts.Where(p => p < 1 || p > 48))}");

                // Build the query string for 802.1Q VLAN creation
                var queryParams = new List<string>
                {
                    $"vid={vid}",
                    $"vname={Uri.EscapeDataString(vlanName ?? $"VLAN{vid}")}"
                };
                
                // Add port selection parameters (1-48 typical range)
                for (int i = 1; i <= 48; i++) // Assuming 48 ports max, adjust if needed
                {
                    string selType;
                    if (taggedPorts?.Contains(i) == true)
                    {
                        selType = "1"; // Tagged
                    }
                    else if (untaggedPorts?.Contains(i) == true)
                    {
                        selType = "0"; // Untagged  
                    }
                    else
                    {
                        selType = "2"; // Not member
                    }
                    queryParams.Add($"selType_{i}={selType}");
                }
                
                queryParams.Add("qvlan_add=Add%2FModify");
                
                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}/qvlanSet.cgi?{queryString}";
                
                var resp = await _http.GetAsync(url);
                
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {resp.StatusCode}: {resp.ReasonPhrase} - {responseContent}");
                }

                var html = await resp.Content.ReadAsStringAsync();
                
                // Check for success (802.1Q endpoint may have different success indicators)
                // We'll consider it successful if we don't get an HTTP error
                // More specific success checking can be added based on actual response format
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new Exception($"Failed to create 802.1Q VLAN: {ex.Message}", ex);
            }
        }

        public async Task DeletePortBasedVlanAsync(int[] vids)
        {
            try
            {
                if (vids == null || vids.Length == 0)
                    throw new ArgumentException("At least one VLAN ID must be specified.");

                // Build query string for 802.1Q VLAN delete operation
                var queryParams = new List<string>();
                
                // Add each VLAN ID as a separate selVlans parameter
                foreach (var vid in vids)
                {
                    queryParams.Add($"selVlans={vid}");
                }
                
                queryParams.Add("qvlan_del=Delete");
                
                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}/qvlanSet.cgi?{queryString}";
                
                Console.WriteLine($"Sending GET request to: {url}");
                
                var resp = await _http.GetAsync(url);
                
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {resp.StatusCode}: {resp.ReasonPhrase} - {responseContent}");
                }

                var html = await resp.Content.ReadAsStringAsync();
                
                // For 802.1Q VLANs, we consider it successful if we don't get an HTTP error
                // More specific success checking can be added based on actual response format
                Console.WriteLine($"✓ VLAN(s) {string.Join(",", vids)} delete request sent successfully");
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new Exception($"Failed to delete VLAN(s): {ex.Message}", ex);
            }
        }

        public async Task SetPvidAsync(int[] ports, int pvid)
        {
            try
            {
                if (ports == null || ports.Length == 0)
                    throw new ArgumentException("At least one port must be specified.");

                if (pvid < 1 || pvid > 4094)
                    throw new ArgumentException($"Invalid PVID: {pvid}. Must be between 1 and 4094.");

                if (ports.Any(p => p < 1 || p > 48))
                    throw new ArgumentException($"Invalid port number(s): {string.Join(",", ports.Where(p => p < 1 || p > 48))}");

                // Convert port array to bitmask for the pbm parameter
                uint portBitmask = 0;
                foreach (var port in ports)
                {
                    portBitmask |= (uint)(1 << (port - 1)); // Port numbers are 1-based, bitmask is 0-based
                }

                var queryParams = new List<string>
                {
                    $"pbm={portBitmask}",
                    $"pvid={pvid}"
                };

                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}/vlanPvidSet.cgi?{queryString}";

                Console.WriteLine($"Setting PVID {pvid} for ports {string.Join(",", ports)}: {url}");

                var resp = await _http.GetAsync(url);

                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"HTTP {resp.StatusCode}: {resp.ReasonPhrase} - {responseContent}");
                }

                Console.WriteLine($"✓ PVID {pvid} set successfully for ports {string.Join(",", ports)}");
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                throw new Exception($"Failed to set PVID: {ex.Message}", ex);
            }
        }

        public async Task RebootAsync(bool saveConfig = false)
        {
            var form = new Dictionary<string, string>
            {
                ["reboot_op"] = "reboot",
                ["save_op"] = saveConfig.ToString().ToLower()
            };
            var resp = await _http.PostAsync(_baseUrl + "/reboot.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        // System Management Methods

        public async Task SetSystemNameAsync(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
                throw new ArgumentException("System name cannot be empty.");
            
            if (systemName.Length > 32)
                throw new ArgumentException("System name cannot exceed 32 characters.");

            var form = new Dictionary<string, string>
            {
                ["sysName"] = systemName
            };

            var resp = await _http.PostAsync(_baseUrl + "/system_name_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetIpConfigurationAsync(bool dhcpEnabled, string? ipAddress = null, string? subnetMask = null, string? gateway = null)
        {
            var formData = new MultipartFormDataContent();
            
            formData.Add(new StringContent(dhcpEnabled ? "enable" : "disable"), "dhcpSetting");
            
            if (!dhcpEnabled)
            {
                if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(subnetMask) || string.IsNullOrWhiteSpace(gateway))
                    throw new ArgumentException("IP address, subnet mask, and gateway are required when DHCP is disabled.");
                
                formData.Add(new StringContent(ipAddress), "ip_address");
                formData.Add(new StringContent(subnetMask), "ip_netmask");
                formData.Add(new StringContent(gateway), "ip_gateway");
            }

            var resp = await _http.PostAsync(_baseUrl + "/ip_setting.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        public async Task FactoryResetAsync()
        {
            var form = new Dictionary<string, string>
            {
                ["reset_op"] = "factory"
            };

            var resp = await _http.PostAsync(_baseUrl + "/reset.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SaveConfigurationAsync()
        {
            var form = new Dictionary<string, string>
            {
                ["action_op"] = "save"
            };

            var resp = await _http.PostAsync(_baseUrl + "/savingconfig.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetLedControlAsync(bool ledEnabled)
        {
            var form = new Dictionary<string, string>
            {
                ["rd_led"] = ledEnabled ? "1" : "0"
            };

            var resp = await _http.PostAsync(_baseUrl + "/led_on_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetUserAccountAsync(string newUsername, string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newUsername) || newUsername.Length > 16)
                throw new ArgumentException("Username must be 1-16 characters.");
            
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6 || newPassword.Length > 16)
                throw new ArgumentException("Password must be 6-16 characters.");
            
            if (newPassword != confirmPassword)
                throw new ArgumentException("Password confirmation does not match.");

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(newUsername), "txt_username");
            formData.Add(new StringContent(currentPassword), "txt_oldpwd");
            formData.Add(new StringContent(newPassword), "txt_userpwd");
            formData.Add(new StringContent(confirmPassword), "txt_confirmpwd");

            var resp = await _http.PostAsync(_baseUrl + "/usr_account_set.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        // Port Management Methods

        public async Task ClearPortStatisticsAsync()
        {
            var formData = new MultipartFormDataContent();
            var resp = await _http.PostAsync(_baseUrl + "/port_statistics_set.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        // Advanced Features Methods

        public async Task SetPortMirroringEnabledAsync(bool enabled, int mirrorDestinationPort = 1)
        {
            if (enabled && (mirrorDestinationPort < 1 || mirrorDestinationPort > 48))
                throw new ArgumentException("Invalid mirror destination port.");

            var form = new Dictionary<string, string>
            {
                ["state"] = enabled ? "1" : "0",
                ["mirroringport"] = mirrorDestinationPort.ToString()
            };

            var resp = await _http.PostAsync(_baseUrl + "/mirror_enabled_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task ConfigurePortMirroringAsync(int[] sourcePorts, bool ingressEnabled, bool egressEnabled)
        {
            if (sourcePorts == null || sourcePorts.Length == 0)
                throw new ArgumentException("At least one source port must be specified.");

            var formData = new MultipartFormDataContent();
            
            foreach (var port in sourcePorts)
            {
                formData.Add(new StringContent(port.ToString()), "mirroredport");
            }
            
            formData.Add(new StringContent(ingressEnabled ? "1" : "0"), "ingressState");
            formData.Add(new StringContent(egressEnabled ? "1" : "0"), "egressState");

            var resp = await _http.PostAsync(_baseUrl + "/mirrored_port_set.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetPortTrunkingAsync(int trunkId, int[] memberPorts)
        {
            if (trunkId < 1 || trunkId > 8)
                throw new ArgumentException("Trunk ID must be between 1 and 8.");

            var form = new Dictionary<string, string>
            {
                ["trunk_id"] = trunkId.ToString()
            };

            if (memberPorts != null)
            {
                foreach (var port in memberPorts)
                {
                    form.Add($"member_ports", port.ToString());
                }
            }

            var resp = await _http.PostAsync(_baseUrl + "/port_trunk_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetLoopPreventionAsync(bool enabled)
        {
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(enabled ? "1" : "0"), "lpEn");

            var resp = await _http.PostAsync(_baseUrl + "/loop_prevention_set.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        // QoS Methods

        public async Task SetQosModeAsync(int mode)
        {
            var form = new Dictionary<string, string>
            {
                ["qos_mode"] = mode.ToString()
            };

            var resp = await _http.PostAsync(_baseUrl + "/qos_mode_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetBandwidthControlAsync(int[] ports, int ingressRate, int egressRate)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.");

            var form = new Dictionary<string, string>
            {
                ["ingress_rate"] = ingressRate.ToString(),
                ["egress_rate"] = egressRate.ToString()
            };

            foreach (var port in ports)
            {
                form.Add("portid", port.ToString());
            }

            var resp = await _http.PostAsync(_baseUrl + "/qos_bandwidth_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetPortPriorityAsync(int[] ports, int priority)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.");
            
            if (priority < 0 || priority > 7)
                throw new ArgumentException("Priority must be between 0 and 7.");

            var form = new Dictionary<string, string>
            {
                ["priority"] = priority.ToString()
            };

            foreach (var port in ports)
            {
                form.Add("portid", port.ToString());
            }

            var resp = await _http.PostAsync(_baseUrl + "/qos_port_priority_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetStormControlAsync(int[] ports, int broadcastRate, int multicastRate, int unicastRate)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.");

            var form = new Dictionary<string, string>
            {
                ["broadcast_rate"] = broadcastRate.ToString(),
                ["multicast_rate"] = multicastRate.ToString(),
                ["unicast_rate"] = unicastRate.ToString()
            };

            foreach (var port in ports)
            {
                form.Add("portid", port.ToString());
            }

            var resp = await _http.PostAsync(_baseUrl + "/qos_storm_set.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        // IGMP Snooping

        public async Task SetIgmpSnoopingAsync(bool enabled)
        {
            var form = new Dictionary<string, string>
            {
                ["igmp_enable"] = enabled ? "1" : "0"
            };

            var resp = await _http.PostAsync(_baseUrl + "/igmpSnooping.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        // PoE Management (for PoE-enabled switches)

        public async Task SetPoeGlobalConfigAsync(float powerLimit)
        {
            if (powerLimit <= 0)
                throw new ArgumentException("Power limit must be greater than 0.");

            var form = new Dictionary<string, string>
            {
                ["name_powerlimit"] = powerLimit.ToString("F1")
            };

            var resp = await _http.PostAsync(_baseUrl + "/poe_global_config.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        public async Task SetPoePortConfigAsync(int[] ports, int state = 7, int priority = 7, int powerLimit = 7, float? manualPowerLimit = null)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.");

            var form = new Dictionary<string, string>
            {
                ["name_pstate"] = state.ToString(),
                ["name_ppriority"] = priority.ToString(),
                ["name_ppowerlimit"] = powerLimit.ToString()
            };

            if (powerLimit == 6 && manualPowerLimit.HasValue) // Manual power limit
            {
                if (manualPowerLimit.Value < 0.1f || manualPowerLimit.Value > 30.0f)
                    throw new ArgumentException("Manual power limit must be between 0.1 and 30.0 watts.");
                
                form["name_ppowerlimit2"] = manualPowerLimit.Value.ToString("F1");
            }

            foreach (var port in ports)
            {
                form.Add($"sel_{port}", "1");
            }

            var resp = await _http.PostAsync(_baseUrl + "/poe_port_config.cgi", new FormUrlEncodedContent(form));
            resp.EnsureSuccessStatusCode();
        }

        // Configuration Management

        public async Task<Stream> BackupConfigurationAsync()
        {
            var formData = new MultipartFormDataContent();
            var resp = await _http.PostAsync(_baseUrl + "/config_back.cgi", formData);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStreamAsync();
        }

        public async Task RestoreConfigurationAsync(Stream configFileStream, string fileName)
        {
            if (configFileStream == null)
                throw new ArgumentException("Configuration file stream cannot be null.");
            
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Configuration file must have .cfg extension.");
            
            if (fileName.Length > 64)
                throw new ArgumentException("Configuration filename cannot exceed 64 characters.");

            var formData = new MultipartFormDataContent();
            var fileContent = new StreamContent(configFileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formData.Add(fileContent, "configfile", fileName);

            var resp = await _http.PostAsync(_baseUrl + "/conf_restore.cgi", formData);
            resp.EnsureSuccessStatusCode();
        }

        public async Task UpgradeFirmwareAsync(Stream firmwareFileStream, string fileName)
        {
            if (firmwareFileStream == null)
                throw new ArgumentException("Firmware file stream cannot be null.");
            
            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Firmware file must have .bin extension.");

            var formData = new MultipartFormDataContent();
            var fileContent = new StreamContent(firmwareFileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formData.Add(fileContent, "firmware", fileName);

            var resp = await _http.PostAsync(_baseUrl + "/httpupg.cgi?cmd=fw_upgrade", formData);
            resp.EnsureSuccessStatusCode();
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }

    public class VlanConfigData
    {
        public bool State { get; set; }
        public int PortNum { get; set; } = 24;
        public int Count { get; set; }
        public int[] VlanIds { get; set; } = Array.Empty<int>();
        public int[] Members { get; set; } = Array.Empty<int>();
    }

    public class CableDiagnosticResult
    {
        public int[] CableStates { get; set; } = Array.Empty<int>();
        public int[] CableLengths { get; set; } = Array.Empty<int>();
        public int MaxPorts { get; set; } = 24;

        private static readonly string[] StateDescriptions = 
        {
            "No Cable",    // 0
            "Normal",      // 1
            "Open",        // 2
            "Short",       // 3
            "Open & Short", // 4
            "Cross Cable"  // 5
        };

        public string FormatResults()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Cable Diagnostic Results ===");
            sb.AppendLine("Port | Status      | Length (m)");
            sb.AppendLine("-----|-------------|----------");

            for (int i = 0; i < MaxPorts && i < CableStates.Length; i++)
            {
                var portNum = i + 1;
                var state = CableStates[i];
                var length = i < CableLengths.Length ? CableLengths[i] : -1;

                string stateDesc;
                if (state == -1)
                {
                    stateDesc = "--";
                }
                else if (state >= 0 && state < StateDescriptions.Length)
                {
                    stateDesc = StateDescriptions[state];
                }
                else
                {
                    stateDesc = "Others";
                }

                var lengthStr = length >= 0 ? length.ToString() : "--";

                sb.AppendLine($"{portNum,4} | {stateDesc,-11} | {lengthStr,8}");
            }

            return sb.ToString();
        }

        public List<PortDiagnostic> GetPortDiagnostics()
        {
            var diagnostics = new List<PortDiagnostic>();
            
            for (int i = 0; i < MaxPorts && i < CableStates.Length; i++)
            {
                var portNum = i + 1;
                var state = CableStates[i];
                var length = i < CableLengths.Length ? CableLengths[i] : -1;

                string stateDesc;
                if (state == -1)
                {
                    stateDesc = "--";
                }
                else if (state >= 0 && state < StateDescriptions.Length)
                {
                    stateDesc = StateDescriptions[state];
                }
                else
                {
                    stateDesc = "Others";
                }

                diagnostics.Add(new PortDiagnostic
                {
                    PortNumber = portNum,
                    State = state,
                    StateDescription = stateDesc,
                    Length = length
                });
            }

            return diagnostics;
        }
    }

    public class PortDiagnostic
    {
        public int PortNumber { get; set; }
        public int State { get; set; }
        public string StateDescription { get; set; } = string.Empty;
        public int Length { get; set; }

        public bool IsHealthy => State == 1; // Normal state
        public bool HasIssue => State >= 2 && State <= 5; // Open, Short, Open & Short, Cross Cable
        public bool IsUntested => State == -1;
        public bool IsDisconnected => State == 0;
    }
}