using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using TPLinkWebUI.Constants;
using TPLinkWebUI.Configuration;

namespace TPLinkWebUI.Services
{
    /// <summary>
    /// Client for communicating with TPLink switches via HTTP
    /// </summary>
    public class TplinkClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger<TplinkClient>? _logger;
        private readonly SwitchConfiguration _switchConfig;

        /// <summary>
        /// Initializes a new instance of TplinkClient
        /// </summary>
        /// <param name="baseUrl">Base URL of the switch</param>
        /// <param name="username">Username for authentication</param>
        /// <param name="password">Password for authentication</param>
        /// <param name="logger">Optional logger instance</param>
        /// <param name="switchConfig">Optional switch configuration</param>
        public TplinkClient(string baseUrl, string username, string password, ILogger<TplinkClient>? logger = null, SwitchConfiguration? switchConfig = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;
            _logger = logger;
            _switchConfig = switchConfig ?? new SwitchConfiguration();
            
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AllowAutoRedirect = true
            };
            
            var timeoutSeconds = _switchConfig.ConnectionTimeoutSeconds;
            _http = new HttpClient(handler) 
            { 
                Timeout = TimeSpan.FromSeconds(timeoutSeconds) 
            };
            
            _logger?.LogDebug("TplinkClient initialized for {Host} with timeout {TimeoutSeconds}s", 
                GetSafeUrl(_baseUrl), timeoutSeconds);
        }

        /// <summary>
        /// Gets a safe version of the URL for logging (removes sensitive information)
        /// </summary>
        private static string GetSafeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            }
            catch
            {
                return "[INVALID_URL]";
            }
        }

        /// <summary>
        /// Sets the session cookie for maintaining authentication
        /// </summary>
        /// <param name="cookieValue">Session cookie value</param>
        public void SetSessionCookie(string cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                _logger?.LogWarning("Attempted to set empty session cookie");
                return;
            }

            try
            {
                var uri = new Uri(_baseUrl);
                var cookie = new Cookie("SessionID", cookieValue, "/", uri.Host);
                _cookieContainer.Add(cookie);
                _logger?.LogDebug("Session cookie set for {Host}", GetSafeUrl(_baseUrl));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set session cookie for {Host}", GetSafeUrl(_baseUrl));
                throw new InvalidOperationException("Failed to set session cookie", ex);
            }
        }

        /// <summary>
        /// Gets the current session cookie value
        /// </summary>
        /// <returns>Session cookie value or null if not found</returns>
        public string? GetSessionCookie()
        {
            try
            {
                var uri = new Uri(_baseUrl);
                var cookies = _cookieContainer.GetCookies(uri);
                return cookies["SessionID"]?.Value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get session cookie for {Host}", GetSafeUrl(_baseUrl));
                return null;
            }
        }

        /// <summary>
        /// Checks if the client is currently logged in to the switch
        /// </summary>
        /// <returns>True if logged in, false otherwise</returns>
        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                var timeoutSeconds = _switchConfig.LoginTimeoutSeconds;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var resp = await _http.GetAsync(_baseUrl + SwitchEndpoints.SystemInfo, cts.Token);
                var isLoggedIn = resp.IsSuccessStatusCode;
                
                _logger?.LogDebug("Login status check for {Host}: {Status}", 
                    GetSafeUrl(_baseUrl), isLoggedIn ? "authenticated" : "not authenticated");
                
                return isLoggedIn;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Login status check timed out for {Host}", GetSafeUrl(_baseUrl));
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Login status check failed for {Host}", GetSafeUrl(_baseUrl));
                return false;
            }
        }

        /// <summary>
        /// Tests basic connectivity to the switch
        /// </summary>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var timeoutSeconds = _switchConfig.TestConnectionTimeoutSeconds;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var resp = await _http.GetAsync(_baseUrl + "/", cts.Token);
                var canConnect = resp.IsSuccessStatusCode;
                
                _logger?.LogDebug("Connection test for {Host}: {Status}", 
                    GetSafeUrl(_baseUrl), canConnect ? "successful" : "failed");
                
                return canConnect;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Connection test timed out for {Host}", GetSafeUrl(_baseUrl));
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Connection test failed for {Host}", GetSafeUrl(_baseUrl));
                return false;
            }
        }

        /// <summary>
        /// Authenticates with the switch using provided credentials
        /// </summary>
        /// <exception cref="SwitchConnectionException">Thrown when connection or authentication fails</exception>
        /// <exception cref="SwitchAuthenticationException">Thrown when authentication fails specifically</exception>
        /// <exception cref="SwitchTimeoutException">Thrown when operations timeout</exception>
        public async Task LoginAsync()
        {
            _logger?.LogInformation("Attempting to login to switch at {Host}", GetSafeUrl(_baseUrl));
            
            try
            {
                // First test basic connectivity
                if (!await TestConnectionAsync())
                {
                    var message = $"Cannot connect to switch at {GetSafeUrl(_baseUrl)}. Please check the IP address and network connectivity.";
                    _logger?.LogError("Connection test failed during login for {Host}", GetSafeUrl(_baseUrl));
                    throw new SwitchConnectionException(message);
                }

                var resp = await _http.GetAsync(_baseUrl + "/");
                var html = await resp.Content.ReadAsStringAsync();
                
                if (html.Contains(ResponsePatterns.LoginCheck))
                {
                    _logger?.LogDebug("Login form detected, submitting credentials for {Host}", GetSafeUrl(_baseUrl));
                    
                    var form = new Dictionary<string, string>
                    {
                        ["username"] = _username,
                        ["password"] = _password,
                        ["logon"] = "Login"
                    };
                    
                    var login = await _http.PostAsync(_baseUrl + SwitchEndpoints.Login, new FormUrlEncodedContent(form));
                    if (!login.IsSuccessStatusCode)
                    {
                        var test = await _http.GetAsync(_baseUrl + SwitchEndpoints.SystemInfo);
                        if (!test.IsSuccessStatusCode)
                        {
                            _logger?.LogError("Login failed for {Host} - HTTP {StatusCode}", GetSafeUrl(_baseUrl), login.StatusCode);
                            throw new SwitchAuthenticationException("Login failed - please check username and password");
                        }
                    }
                }
                
                // Verify we can access system info after login
                if (!await IsLoggedInAsync())
                {
                    _logger?.LogError("Login verification failed for {Host}", GetSafeUrl(_baseUrl));
                    throw new SwitchAuthenticationException("Login appeared successful but cannot access system information");
                }
                
                _logger?.LogInformation("Successfully logged in to switch at {Host}", GetSafeUrl(_baseUrl));
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger?.LogError(ex, "Login timeout for {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchTimeoutException("Connection timeout - switch may be unreachable or slow to respond");
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during login for {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error: {ex.Message}", ex);
            }
            catch (SwitchConnectionException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (SwitchAuthenticationException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during login for {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error during login: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves system information from the switch
        /// </summary>
        /// <returns>Formatted system information string</returns>
        /// <exception cref="SwitchConnectionException">Thrown when unable to retrieve system information</exception>
        public async Task<string> GetSystemInfoAsync()
        {
            _logger?.LogDebug("Retrieving system information from {Host}", GetSafeUrl(_baseUrl));
            
            try
            {
                var resp = await _http.GetAsync(_baseUrl + SwitchEndpoints.SystemInfo);
                resp.EnsureSuccessStatusCode();
                var html = await resp.Content.ReadAsStringAsync();

                // Array-style parsing
                var matchArray = RegexPatterns.SystemInfoArrayPattern.Match(html);
                if (matchArray.Success)
                {
                    var parts = matchArray.Groups[1].Value.Split(',');
                    if (parts.Length >= 7)
                    {
                        var result = $"Device Description: {parts[0].Trim(' ', '"')}\n" +
                               $"MAC Address: {parts[1].Trim(' ', '"')}\n" +
                               $"IP Address: {parts[2].Trim(' ', '"')}\n" +
                               $"Subnet Mask: {parts[3].Trim(' ', '"')}\n" +
                               $"Gateway: {parts[4].Trim(' ', '"')}\n" +
                               $"Firmware Version: {parts[5].Trim(' ', '"')}\n" +
                               $"Hardware Version: {parts[6].Trim(' ', '"')}";
                        
                        _logger?.LogDebug("System info retrieved using array format from {Host}", GetSafeUrl(_baseUrl));
                        return result;
                    }
                }

                // Object-literal parsing
                var infoMatch = RegexPatterns.SystemInfoObjectPattern.Match(html);
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
                        var result = $"Device Description: {dict.GetValueOrDefault("descriStr")}\n" +
                               $"MAC Address: {dict.GetValueOrDefault("macStr")}\n" +
                               $"IP Address: {dict.GetValueOrDefault("ipStr")}\n" +
                               $"Subnet Mask: {dict.GetValueOrDefault("netmaskStr")}\n" +
                               $"Gateway: {dict.GetValueOrDefault("gatewayStr")}\n" +
                               $"Firmware Version: {dict.GetValueOrDefault("firmwareStr")}\n" +
                               $"Hardware Version: {dict.GetValueOrDefault("hardwareStr")}";
                        
                        _logger?.LogDebug("System info retrieved using object format from {Host}", GetSafeUrl(_baseUrl));
                        return result;
                    }
                }

                // Fallback: raw HTML
                _logger?.LogWarning("Could not parse system info from {Host}, returning raw HTML", GetSafeUrl(_baseUrl));
                return html;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to retrieve system info from {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Failed to retrieve system information: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error retrieving system info from {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error retrieving system information: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves port information from the switch
        /// </summary>
        /// <returns>Formatted port information string</returns>
        /// <exception cref="SwitchConnectionException">Thrown when unable to retrieve port information</exception>
        public async Task<string> GetPortInfoAsync()
        {
            _logger?.LogDebug("Retrieving port information from {Host}", GetSafeUrl(_baseUrl));
            
            try
            {
                var resp = await _http.GetAsync(_baseUrl + SwitchEndpoints.PortSettings);
                resp.EnsureSuccessStatusCode();
                var html = await resp.Content.ReadAsStringAsync();

                // Extract the all_info JavaScript object
                var allInfoMatch = RegexPatterns.PortInfoPattern.Match(html);
                if (!allInfoMatch.Success)
                {
                    _logger?.LogError("Could not parse port information from response for {Host}", GetSafeUrl(_baseUrl));
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
                var maxPortMatch = RegexPatterns.MaxPortPattern.Match(html);
                var maxPorts = maxPortMatch.Success ? int.Parse(maxPortMatch.Groups[1].Value) : NetworkConstants.DefaultMaxPorts;

                _logger?.LogDebug("Parsed port info for {MaxPorts} ports from {Host}", maxPorts, GetSafeUrl(_baseUrl));

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
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to retrieve port info from {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Failed to retrieve port information: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error retrieving port info from {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error retrieving port information: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses JavaScript array from content using optimized regex patterns
        /// </summary>
        /// <param name="content">JavaScript content to parse</param>
        /// <param name="arrayName">Name of the array to extract</param>
        /// <returns>Array of parsed integers</returns>
        private int[] ParseJsArray(string content, string arrayName)
        {
            try
            {
                var pattern = RegexPatterns.CreateJsArrayPattern(arrayName);
                var match = pattern.Match(content);
                if (!match.Success) 
                {
                    _logger?.LogDebug("Could not find JavaScript array {ArrayName} in content", arrayName);
                    return Array.Empty<int>();
                }
                
                var arrayContent = match.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(arrayContent)) 
                {
                    return Array.Empty<int>();
                }
                
                return Array.ConvertAll(arrayContent.Split(','), s => int.TryParse(s.Trim(), out var n) ? n : 0);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse JavaScript array {ArrayName}", arrayName);
                return Array.Empty<int>();
            }
        }

        /// <summary>
        /// Retrieves VLAN configuration from the switch
        /// </summary>
        /// <returns>Formatted VLAN configuration string</returns>
        public async Task<string> GetVlanConfigurationAsync()
        {
            _logger?.LogDebug("Retrieving VLAN configuration from {Host}", GetSafeUrl(_baseUrl));
            
            try
            {
                var response = await _http.GetStringAsync(_baseUrl + SwitchEndpoints.VlanConfig);
                
                var vlanData = ParseVlanData(response);
                
                if (!vlanData.State)
                {
                    _logger?.LogDebug("Port-based VLAN is disabled on {Host}", GetSafeUrl(_baseUrl));
                    return "Port-based VLAN is disabled.";
                }
                
                var result = new StringBuilder();
                result.AppendLine("=== Port-based VLAN Configuration ===");
                result.AppendLine($"Status: Enabled");
                result.AppendLine($"Total Ports: {vlanData.PortNum}");
                result.AppendLine($"Number of VLANs: {vlanData.Count}");
                result.AppendLine();
                
                if (vlanData.Count > 0 && vlanData.VlanIds.Length > 0 && vlanData.Members.Length > 0)
                {
                    result.AppendLine("VLAN ID | Member Ports");
                    result.AppendLine("--------|-------------");
                    
                    for (int i = 0; i < Math.Min(vlanData.VlanIds.Length, vlanData.Members.Length); i++)
                    {
                        var vlanId = vlanData.VlanIds[i];
                        var portRange = ConvertBitmaskToPortRange(vlanData.Members[i]);
                        result.AppendLine($"{vlanId,7} | {portRange}");
                    }
                    
                    _logger?.LogDebug("Retrieved {VlanCount} VLANs from {Host}", vlanData.Count, GetSafeUrl(_baseUrl));
                }
                else
                {
                    result.AppendLine("No VLANs configured.");
                    _logger?.LogDebug("No VLANs configured on {Host}", GetSafeUrl(_baseUrl));
                }
                
                return result.ToString();
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to retrieve VLAN configuration from {Host}", GetSafeUrl(_baseUrl));
                return $"Error retrieving VLAN configuration: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error retrieving VLAN configuration from {Host}", GetSafeUrl(_baseUrl));
                return $"Error retrieving VLAN configuration: {ex.Message}";
            }
        }

        /// <summary>
        /// Parses VLAN data from HTML response using optimized regex patterns
        /// </summary>
        /// <param name="html">HTML response from switch</param>
        /// <returns>Parsed VLAN configuration data</returns>
        private VlanConfigData ParseVlanData(string html)
        {
            var data = new VlanConfigData();
            
            try
            {
                // Extract the JavaScript object using optimized regex patterns
                var stateMatch = RegexPatterns.VlanStatePattern.Match(html);
                data.State = stateMatch.Success && stateMatch.Groups[1].Value == "1";
                
                var portNumMatch = RegexPatterns.VlanPortNumPattern.Match(html);
                if (portNumMatch.Success)
                    data.PortNum = int.Parse(portNumMatch.Groups[1].Value);
                
                var countMatch = RegexPatterns.VlanCountPattern.Match(html);
                if (countMatch.Success)
                    data.Count = int.Parse(countMatch.Groups[1].Value);
                
                // Parse VLAN IDs array
                var vidsMatch = RegexPatterns.VlanIdsPattern.Match(html);
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
                var mbrsMatch = RegexPatterns.VlanMembersPattern.Match(html);
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
                    _logger?.LogWarning("VLAN IDs and Members arrays have different lengths for {Host}, truncating to {MinLength}", 
                        GetSafeUrl(_baseUrl), minLength);
                    data.VlanIds = data.VlanIds.Take(minLength).ToArray();
                    data.Members = data.Members.Take(minLength).ToArray();
                }
                
                // Update count to match actual data
                data.Count = data.VlanIds.Length;
                
                _logger?.LogDebug("Parsed VLAN data: State={State}, Count={Count}, PortNum={PortNum} for {Host}", 
                    data.State, data.Count, data.PortNum, GetSafeUrl(_baseUrl));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse VLAN data from {Host}, returning empty data", GetSafeUrl(_baseUrl));
                data = new VlanConfigData();
            }
            
            return data;
        }

        /// <summary>
        /// Converts a port bitmask to a human-readable port range string
        /// </summary>
        /// <param name="bitmask">Port bitmask value</param>
        /// <returns>Formatted port range string (e.g., "1,3-5,8")</returns>
        private string ConvertBitmaskToPortRange(int bitmask)
        {
            var result = "";
            var mask = 1;
            
            for (int port = 1; port <= NetworkConstants.MaxPortBitmaskSize; port++)
            {
                if ((bitmask & mask) != 0)
                {
                    var startPort = port;
                    var endPort = port;
                    
                    // Find consecutive ports
                    mask <<= 1;
                    while ((bitmask & mask) != 0 && ++port <= NetworkConstants.MaxPortBitmaskSize)
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
                    
                    if (port < NetworkConstants.MaxPortBitmaskSize)
                        result += ",";
                    
                    continue;
                }
                mask <<= 1;
            }
            
            return result.TrimEnd(',');
        }

        /// <summary>
        /// Configures a specific port on the switch
        /// </summary>
        /// <param name="port">Port number (1-48)</param>
        /// <param name="enable">Enable or disable the port</param>
        /// <param name="speed">Speed configuration (1=Auto, 2=10MH, etc.)</param>
        /// <param name="flowControl">Enable flow control</param>
        /// <exception cref="ArgumentException">Thrown when port number is invalid</exception>
        /// <exception cref="SwitchConnectionException">Thrown when configuration fails</exception>
        public async Task SetPortConfigAsync(int port, bool enable, int speed = 1, bool flowControl = false)
        {
            // Validate port number
            if (port < NetworkConstants.MinPortNumber || port > NetworkConstants.MaxSupportedPorts)
                throw new ArgumentException($"Invalid port number: {port}. Must be between {NetworkConstants.MinPortNumber} and {NetworkConstants.MaxSupportedPorts}.", nameof(port));

            _logger?.LogInformation("Configuring port {Port}: Enable={Enable}, Speed={Speed}, FlowControl={FlowControl} on {Host}", 
                port, enable, speed, flowControl, GetSafeUrl(_baseUrl));

            try
            {
                var form = new Dictionary<string, string>
                {
                    ["portid"] = port.ToString() + "^",
                    ["state"] = enable ? "1^" : "0^",
                    ["speed"] = speed.ToString() + "^",
                    ["flowcontrol"] = flowControl ? "1^" : "0^",
                    ["apply"] = "Apply"
                };

                var resp = await _http.PostAsync(_baseUrl + SwitchEndpoints.PortConfig, new FormUrlEncodedContent(form));
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    _logger?.LogError("Port {Port} configuration failed on {Host}: HTTP {StatusCode} - {Response}", 
                        port, GetSafeUrl(_baseUrl), resp.StatusCode, responseContent);
                    throw new SwitchConnectionException($"Failed to configure port {port}: HTTP {resp.StatusCode}");
                }
                
                _logger?.LogInformation("Port {Port} configured successfully on {Host}", port, GetSafeUrl(_baseUrl));
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error configuring port {Port} on {Host}", port, GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error configuring port {port}: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is SwitchConnectionException))
            {
                _logger?.LogError(ex, "Unexpected error configuring port {Port} on {Host}", port, GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error configuring port {port}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Runs cable diagnostics on specified ports
        /// </summary>
        /// <param name="ports">Array of port numbers to test</param>
        /// <returns>Cable diagnostic results</returns>
        /// <exception cref="ArgumentException">Thrown when port numbers are invalid</exception>
        /// <exception cref="SwitchConnectionException">Thrown when diagnostics fail</exception>
        public async Task<CableDiagnosticResult> RunCableDiagnosticsAsync(int[] ports)
        {
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.", nameof(ports));
        
            var invalidPorts = ports.Where(p => p < NetworkConstants.MinPortNumber || p > NetworkConstants.MaxSupportedPorts).ToArray();
            if (invalidPorts.Any())
                throw new ArgumentException($"Invalid port number(s): {string.Join(",", invalidPorts)}. Must be between {NetworkConstants.MinPortNumber} and {NetworkConstants.MaxSupportedPorts}.", nameof(ports));

            _logger?.LogInformation("Running cable diagnostics on ports {Ports} for {Host}", 
                string.Join(",", ports), GetSafeUrl(_baseUrl));

            try
            {
                // Build the query string parameters
                var queryParams = new List<string>();
                foreach (var port in ports)
                {
                    queryParams.Add($"chk_{port}={port}^");
                }
                queryParams.Add("Apply=Apply");

                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}{SwitchEndpoints.CableDiagnostic}?{queryString}";

                var resp = await _http.GetAsync(url);
        
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    _logger?.LogError("Cable diagnostics failed for ports {Ports} on {Host}: HTTP {StatusCode} - {Response}", 
                        string.Join(",", ports), GetSafeUrl(_baseUrl), resp.StatusCode, responseContent);
                    throw new SwitchConnectionException($"Cable diagnostics failed: HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
                }

                var html = await resp.Content.ReadAsStringAsync();
                var result = ParseCableDiagnosticResults(html);
                
                _logger?.LogInformation("Cable diagnostics completed for {PortCount} ports on {Host}", 
                    ports.Length, GetSafeUrl(_baseUrl));
                
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during cable diagnostics for ports {Ports} on {Host}", 
                    string.Join(",", ports), GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error during cable diagnostics: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is SwitchConnectionException))
            {
                _logger?.LogError(ex, "Unexpected error during cable diagnostics for ports {Ports} on {Host}", 
                    string.Join(",", ports), GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error during cable diagnostics: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses cable diagnostic results from HTML response
        /// </summary>
        /// <param name="html">HTML response from cable diagnostics</param>
        /// <returns>Parsed cable diagnostic results</returns>
        private CableDiagnosticResult ParseCableDiagnosticResults(string html)
        {
            var result = new CableDiagnosticResult();

            try
            {
                // Extract cable state array
                var stateMatch = RegexPatterns.CableStatePattern.Match(html);
                if (stateMatch.Success)
                {
                    var stateValues = stateMatch.Groups[1].Value.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out var val) ? val : -1)
                        .ToArray();
                    result.CableStates = stateValues;
                    _logger?.LogDebug("Parsed {StateCount} cable states", stateValues.Length);
                }

                // Extract cable length array
                var lengthMatch = RegexPatterns.CableLengthPattern.Match(html);
                if (lengthMatch.Success)
                {
                    var lengthValues = lengthMatch.Groups[1].Value.Split(',')
                        .Select(s => int.TryParse(s.Trim(), out var val) ? val : -1)
                        .ToArray();
                    result.CableLengths = lengthValues;
                    _logger?.LogDebug("Parsed {LengthCount} cable lengths", lengthValues.Length);
                }

                // Extract max port count
                var maxPortMatch = RegexPatterns.CableMaxPortPattern.Match(html);
                if (maxPortMatch.Success)
                {
                    result.MaxPorts = int.Parse(maxPortMatch.Groups[1].Value);
                }
                else
                {
                    result.MaxPorts = NetworkConstants.DefaultMaxPorts;
                }
                
                _logger?.LogDebug("Cable diagnostic parsing completed for {MaxPorts} ports", result.MaxPorts);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse cable diagnostic results, returning empty result");
                result = new CableDiagnosticResult();
            }

            return result;
        }

        /// <summary>
        /// Creates a port-based VLAN on the switch
        /// </summary>
        /// <param name="vid">VLAN ID (1-4094)</param>
        /// <param name="ports">Array of port numbers to include in VLAN</param>
        /// <exception cref="ArgumentException">Thrown when VLAN ID or port numbers are invalid</exception>
        /// <exception cref="SwitchConnectionException">Thrown when VLAN creation fails</exception>
        public async Task CreatePortBasedVlanAsync(int vid, int[] ports)
        {
            // Validate VLAN ID
            if (vid < VlanConstants.MinVlanId || vid > VlanConstants.MaxVlanId)
                throw new ArgumentException($"Invalid VLAN ID: {vid}. Must be between {VlanConstants.MinVlanId} and {VlanConstants.MaxVlanId}.", nameof(vid));
            
            if (ports == null || ports.Length == 0)
                throw new ArgumentException("At least one port must be specified.", nameof(ports));
            
            var invalidPorts = ports.Where(p => p < NetworkConstants.MinPortNumber || p > NetworkConstants.MaxSupportedPorts).ToArray();
            if (invalidPorts.Any())
                throw new ArgumentException($"Invalid port number(s): {string.Join(",", invalidPorts)}. Must be between {NetworkConstants.MinPortNumber} and {NetworkConstants.MaxSupportedPorts}.", nameof(ports));

            _logger?.LogInformation("Creating VLAN {VlanId} with ports {Ports} on {Host}", 
                vid, string.Join(",", ports), GetSafeUrl(_baseUrl));

            try
            {
                // Build the correct query string manually to match the working curl format
                var queryParams = new List<string>
                {
                    $"vid={vid}^"
                };
                
                // Add each port as a separate selPorts parameter
                foreach (var port in ports)
                {
                    queryParams.Add($"selPorts={port}^");
                }
                
                queryParams.Add("pvlan_add=Apply");
                
                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}{SwitchEndpoints.VlanSet}?{queryString}";
                
                var resp = await _http.GetAsync(url);
                
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    _logger?.LogError("VLAN {VlanId} creation failed on {Host}: HTTP {StatusCode} - {Response}", 
                        vid, GetSafeUrl(_baseUrl), resp.StatusCode, responseContent);
                    throw new SwitchConnectionException($"VLAN creation failed: HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
                }

                var html = await resp.Content.ReadAsStringAsync();
                
                // Check for success by looking for the success message
                if (!html.Contains(ResponsePatterns.OperationSuccessful))
                {
                    _logger?.LogWarning("VLAN {VlanId} creation on {Host} may have failed - no success message found", 
                        vid, GetSafeUrl(_baseUrl));
                    throw new SwitchConnectionException("VLAN creation may have failed - no success message found in response");
                }
                
                _logger?.LogInformation("VLAN {VlanId} created successfully on {Host}", vid, GetSafeUrl(_baseUrl));
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error creating VLAN {VlanId} on {Host}", vid, GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error creating VLAN: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is SwitchConnectionException))
            {
                _logger?.LogError(ex, "Unexpected error creating VLAN {VlanId} on {Host}", vid, GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Failed to create VLAN: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes port-based VLANs from the switch
        /// </summary>
        /// <param name="vids">Array of VLAN IDs to delete</param>
        /// <exception cref="ArgumentException">Thrown when VLAN IDs are invalid</exception>
        /// <exception cref="SwitchConnectionException">Thrown when VLAN deletion fails</exception>
        public async Task DeletePortBasedVlanAsync(int[] vids)
        {
            if (vids == null || vids.Length == 0)
                throw new ArgumentException("At least one VLAN ID must be specified.", nameof(vids));

            var invalidVids = vids.Where(v => v < VlanConstants.MinVlanId || v > VlanConstants.MaxVlanId).ToArray();
            if (invalidVids.Any())
                throw new ArgumentException($"Invalid VLAN ID(s): {string.Join(",", invalidVids)}. Must be between {VlanConstants.MinVlanId} and {VlanConstants.MaxVlanId}.", nameof(vids));

            _logger?.LogInformation("Deleting VLANs {VlanIds} on {Host}", 
                string.Join(",", vids), GetSafeUrl(_baseUrl));

            try
            {
                // Build query string for delete operation
                var queryParams = new List<string>();
                
                // Add each VLAN ID as a separate selVlans parameter
                foreach (var vid in vids)
                {
                    queryParams.Add($"selVlans={vid}^");
                }
                
                queryParams.Add("pvlan_del=Delete");
                
                var queryString = string.Join("&", queryParams);
                var url = $"{_baseUrl}{SwitchEndpoints.VlanSet}?{queryString}";
                
                _logger?.LogDebug("Sending VLAN delete request to {Host}", GetSafeUrl(_baseUrl));
                
                var resp = await _http.GetAsync(url);
                
                if (!resp.IsSuccessStatusCode)
                {
                    var responseContent = await resp.Content.ReadAsStringAsync();
                    _logger?.LogError("VLAN deletion failed for {VlanIds} on {Host}: HTTP {StatusCode} - {Response}", 
                        string.Join(",", vids), GetSafeUrl(_baseUrl), resp.StatusCode, responseContent);
                    throw new SwitchConnectionException($"VLAN deletion failed: HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
                }

                var html = await resp.Content.ReadAsStringAsync();
                
                // Check for success
                if (html.Contains(ResponsePatterns.OperationSuccessful))
                {
                    _logger?.LogInformation("VLANs {VlanIds} deleted successfully on {Host}", 
                        string.Join(",", vids), GetSafeUrl(_baseUrl));
                }
                else
                {
                    _logger?.LogWarning("VLAN deletion for {VlanIds} on {Host} may have failed - no success message found", 
                        string.Join(",", vids), GetSafeUrl(_baseUrl));
                    throw new SwitchConnectionException("VLAN deletion may have failed - no success message found in response");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error deleting VLANs {VlanIds} on {Host}", 
                    string.Join(",", vids), GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error deleting VLANs: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is SwitchConnectionException))
            {
                _logger?.LogError(ex, "Unexpected error deleting VLANs {VlanIds} on {Host}", 
                    string.Join(",", vids), GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Failed to delete VLAN(s): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reboots the switch
        /// </summary>
        /// <exception cref="SwitchConnectionException">Thrown when reboot command fails</exception>
        public async Task RebootAsync()
        {
            _logger?.LogWarning("Initiating switch reboot for {Host}", GetSafeUrl(_baseUrl));
            
            try
            {
                var form = new Dictionary<string, string>
                {
                    ["reboot_op"] = "reboot^",
                    ["save_op"] = "false"
                };
                
                var resp = await _http.PostAsync(_baseUrl + SwitchEndpoints.Reboot, new FormUrlEncodedContent(form));
                resp.EnsureSuccessStatusCode();
                
                _logger?.LogWarning("Switch reboot command sent successfully to {Host}", GetSafeUrl(_baseUrl));
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Network error during reboot for {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Network error during reboot: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during reboot for {Host}", GetSafeUrl(_baseUrl));
                throw new SwitchConnectionException($"Unexpected error during reboot: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes the HTTP client and releases resources
        /// </summary>
        public void Dispose()
        {
            _logger?.LogDebug("Disposing TplinkClient for {Host}", GetSafeUrl(_baseUrl));
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

    /// <summary>
    /// Exception thrown when switch connection fails
    /// </summary>
    public class SwitchConnectionException : Exception
    {
        public SwitchConnectionException(string message) : base(message) { }
        public SwitchConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when switch authentication fails
    /// </summary>
    public class SwitchAuthenticationException : Exception
    {
        public SwitchAuthenticationException(string message) : base(message) { }
        public SwitchAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when switch operations timeout
    /// </summary>
    public class SwitchTimeoutException : Exception
    {
        public SwitchTimeoutException(string message) : base(message) { }
        public SwitchTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }
}