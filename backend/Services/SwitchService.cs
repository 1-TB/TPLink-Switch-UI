using System;
using System.Threading.Tasks;
using TPLinkWebUI.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace TPLinkWebUI.Services
{
    public class SwitchService
    {
        private readonly CredentialsStorage _storage;
        private readonly ILogger<SwitchService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private TplinkClient? _client;
        private List<PortInfo>? _lastPortInfo;

        public SwitchService(CredentialsStorage storage, ILogger<SwitchService> logger, IServiceProvider serviceProvider)
        {
            _storage = storage;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task EnsureClientAsync(LoginRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Ensuring client connection for {Username}@{Host}", req.Username, req.Host);
            
            var saved = await _storage.LoadAsync();
            if (saved == null || saved.Host != req.Host || saved.Username != req.Username || saved.Password != req.Password)
            {
                _logger.LogInformation("Creating new client connection for {Username}@{Host}", req.Username, req.Host);
                await _storage.SaveAsync(req);
                _client?.Dispose();
                _client = new TplinkClient($"http://{req.Host}", req.Username, req.Password);
                await _client.LoginAsync();
                stopwatch.Stop();
                _logger.LogInformation("New client connection established for {Username}@{Host} in {ElapsedMs}ms", 
                    req.Username, req.Host, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                if (_client == null)
                {
                    _logger.LogDebug("Recreating client from stored credentials for {Username}@{Host}", saved.Username, saved.Host);
                    _client = new TplinkClient($"http://{saved.Host}", saved.Username, saved.Password);
                }
                
                if (saved.SessionCookie != null && saved.CookieExpiration > DateTime.Now)
                {
                    _logger.LogDebug("Using existing session cookie for {Username}@{Host}", saved.Username, saved.Host);
                    _client.SetSessionCookie(saved.SessionCookie);
                    if (!await _client.IsLoggedInAsync()) 
                    {
                        _logger.LogInformation("Session cookie expired, re-authenticating for {Username}@{Host}", saved.Username, saved.Host);
                        await _client.LoginAsync();
                    }
                }
                else
                {
                    _logger.LogInformation("No valid session cookie, authenticating for {Username}@{Host}", saved.Username, saved.Host);
                    await _client.LoginAsync();
                }
                
                // Update stored session cookie
                var newCookie = _client.GetSessionCookie();
                if (newCookie != null)
                {
                    saved.SessionCookie = newCookie;
                    saved.CookieExpiration = DateTime.Now.AddHours(1); // Assume 1 hour session
                    await _storage.SaveAsync(saved);
                    _logger.LogDebug("Updated session cookie for {Username}@{Host}", saved.Username, saved.Host);
                }
                
                stopwatch.Stop();
                _logger.LogDebug("Client connection ensured for {Username}@{Host} in {ElapsedMs}ms", 
                    saved.Username, saved.Host, stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task EnsureClientFromStorageAsync()
        {
            var saved = await _storage.LoadAsync();
            if (saved == null)
                throw new InvalidOperationException("No saved credentials found. Please login first.");

            // Always try to use existing client first if it exists
            if (_client != null) 
            {
                try
                {
                    // Quick check if client is still responsive
                    if (await _client.IsLoggedInAsync())
                    {
                        return; // Client is still valid
                    }
                }
                catch (Exception ex)
                {
                    // Session validation failed, will recreate client
                    _logger.LogDebug(ex, "Session validation failed, recreating client");
                    _client.Dispose();
                    _client = null;
                }
            }
            
            try
            {
                _client = new TplinkClient($"http://{saved.Host}", saved.Username, saved.Password);
                
                // Always try to login fresh to ensure we have a valid session
                await _client.LoginAsync();
                
                // Update stored session cookie
                var newCookie = _client.GetSessionCookie();
                if (newCookie != null)
                {
                    saved.SessionCookie = newCookie;
                    saved.CookieExpiration = DateTime.Now.AddHours(1);
                    await _storage.SaveAsync(saved);
                }
            }
            catch (Exception ex)
            {
                _client?.Dispose();
                _client = null;
                throw new InvalidOperationException($"Failed to connect to switch at {saved.Host}: {ex.Message}");
            }
        }

        public async Task<SystemInfoResponse> GetSystemInfoAsync()
        {
            await EnsureClientFromStorageAsync();
            var systemInfoText = await _client!.GetSystemInfoAsync();
            var response = SystemInfoResponse.Parse(systemInfoText);
            
            // Log system info to history
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            await historyService.LogSystemInfoAsync(response, "PERIODIC_SNAPSHOT");
            
            return response;
        }

        public async Task<PortInfoResponse> GetPortInfoAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            await EnsureClientFromStorageAsync();
            
            var portInfoText = await _client!.GetPortInfoAsync();
            var response = PortInfoResponse.Parse(portInfoText);
            
            // Log port info to history
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            
            // Check for changes from last port info
            string changeType = "PERIODIC_SNAPSHOT";
            if (_lastPortInfo != null && HasPortChanges(_lastPortInfo, response.Ports))
            {
                changeType = "STATUS_CHANGE";
                _logger.LogInformation("Port status changes detected for {ChangedPorts} ports", 
                    GetChangedPortNumbers(_lastPortInfo, response.Ports).Count);
            }
            
            await historyService.LogPortInfoAsync(response.Ports, _lastPortInfo, changeType);
            _lastPortInfo = response.Ports.ToList();
            
            stopwatch.Stop();
            _logger.LogDebug("Port info retrieved and logged in {ElapsedMs}ms with change type: {ChangeType}", 
                stopwatch.ElapsedMilliseconds, changeType);
            
            return response;
        }

        public async Task<VlanConfigResponse> GetVlanConfigurationAsync()
        {
            await EnsureClientFromStorageAsync();
            var vlanConfigText = await _client!.GetVlanConfigurationAsync();
            var response = VlanConfigResponse.Parse(vlanConfigText);
            
            // Log VLAN configuration to history
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            await historyService.LogVlanInfoAsync(response.Vlans, "PERIODIC_SNAPSHOT");
            
            return response;
        }

        public async Task SetPortConfigAsync(int port, bool enable, int speed = 1, bool flowControl = false)
        {
            await EnsureClientFromStorageAsync();
            
            // Get current port config before change
            var currentPortInfo = await GetPortInfoAsync();
            var oldConfig = currentPortInfo.Ports.FirstOrDefault(p => p.PortNumber == port);
            
            await _client!.SetPortConfigAsync(port, enable, speed, flowControl);
            
            // Get new port config after change and log it
            var newPortInfo = await GetPortInfoAsync();
            var newConfig = newPortInfo.Ports.FirstOrDefault(p => p.PortNumber == port);
            
            if (oldConfig != null && newConfig != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
                await historyService.LogPortConfigChangeAsync(port, oldConfig, newConfig);
            }
        }

        public async Task<CableDiagnosticResponse> RunCableDiagnosticsAsync(int[] ports)
        {
            await EnsureClientFromStorageAsync();
            var result = await _client!.RunCableDiagnosticsAsync(ports);
            var response = CableDiagnosticResponse.FromTplinkResult(result);
            
            // Log cable diagnostics to history
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            await historyService.LogCableDiagnosticsAsync(response.Diagnostics, "MANUAL");
            
            return response;
        }

        public async Task CreateVlanAsync(int vlanId, int[] ports)
        {
            // For backwards compatibility, treat all ports as untagged
            await CreateVlanAsync(vlanId, string.Empty, Array.Empty<int>(), ports);
        }

        public async Task CreateVlanAsync(int vlanId, string vlanName, int[] taggedPorts, int[] untaggedPorts)
        {
            await EnsureClientFromStorageAsync();
            await _client!.Create8021QVlanAsync(vlanId, vlanName, taggedPorts, untaggedPorts);
            
            // Log VLAN creation
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            
            var allPorts = (taggedPorts ?? Array.Empty<int>()).Concat(untaggedPorts ?? Array.Empty<int>()).ToList();
            var vlanInfo = new VlanInfo
            {
                VlanId = vlanId,
                VlanName = vlanName ?? $"VLAN{vlanId}",
                TaggedPorts = (taggedPorts ?? Array.Empty<int>()).ToList(),
                UntaggedPorts = (untaggedPorts ?? Array.Empty<int>()).ToList(),
                PortNumbers = allPorts,
                MemberPorts = string.Join(",", allPorts)
            };
            
            await historyService.LogVlanChangeAsync(vlanInfo, "VLAN_CREATED", $"Created VLAN {vlanId} ({vlanName}) with tagged ports: [{string.Join(", ", taggedPorts ?? Array.Empty<int>())}] and untagged ports: [{string.Join(", ", untaggedPorts ?? Array.Empty<int>())}]");
        }

        public async Task DeleteVlansAsync(int[] vlanIds)
        {
            await EnsureClientFromStorageAsync();
            await _client!.DeletePortBasedVlanAsync(vlanIds);
            
            // Log VLAN deletion
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            
            foreach (var vlanId in vlanIds)
            {
                var vlanInfo = new VlanInfo
                {
                    VlanId = vlanId,
                    PortNumbers = new List<int>(),
                    MemberPorts = ""
                };
                
                await historyService.LogVlanChangeAsync(vlanInfo, "VLAN_DELETED", $"Deleted VLAN {vlanId}");
            }
        }

        public async Task SetPvidAsync(int[] ports, int pvid)
        {
            await EnsureClientFromStorageAsync();
            await _client!.SetPvidAsync(ports, pvid);
            
            // Log PVID change using VlanChangeAsync
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            
            // Create a dummy VLAN info for logging PVID changes
            var vlanInfo = new VlanInfo
            {
                VlanId = pvid,
                VlanName = $"PVID_{pvid}",
                PortNumbers = ports.ToList(),
                MemberPorts = string.Join(",", ports)
            };
            
            var message = $"Set PVID {pvid} for ports: {string.Join(", ", ports)}";
            await historyService.LogVlanChangeAsync(vlanInfo, "PVID_CHANGED", message);
        }

        public async Task RebootSwitchAsync()
        {
            await EnsureClientFromStorageAsync();
            await _client!.RebootAsync();
        }

        private bool HasPortChanges(List<PortInfo> oldPorts, List<PortInfo> newPorts)
        {
            if (oldPorts.Count != newPorts.Count) return true;
            
            foreach (var newPort in newPorts)
            {
                var oldPort = oldPorts.FirstOrDefault(p => p.PortNumber == newPort.PortNumber);
                if (oldPort == null) return true;
                
                if (oldPort.Status != newPort.Status ||
                    oldPort.SpeedActual != newPort.SpeedActual ||
                    oldPort.FlowControlActual != newPort.FlowControlActual)
                {
                    return true;
                }
            }
            
            return false;
        }

        private List<int> GetChangedPortNumbers(List<PortInfo> oldPorts, List<PortInfo> newPorts)
        {
            var changedPorts = new List<int>();
            
            foreach (var newPort in newPorts)
            {
                var oldPort = oldPorts.FirstOrDefault(p => p.PortNumber == newPort.PortNumber);
                if (oldPort == null)
                {
                    changedPorts.Add(newPort.PortNumber);
                    continue;
                }
                
                if (oldPort.Status != newPort.Status ||
                    oldPort.SpeedActual != newPort.SpeedActual ||
                    oldPort.FlowControlActual != newPort.FlowControlActual)
                {
                    changedPorts.Add(newPort.PortNumber);
                }
            }
            
            return changedPorts;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}