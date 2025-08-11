using Microsoft.AspNetCore.Mvc;
using TPLinkWebUI.Models;
using TPLinkWebUI.Services;
using System.Diagnostics;

namespace TPLinkWebUI.Controllers
{
    [ApiController]
    [Route("api")]
    public class SwitchController : ControllerBase
    {
        private readonly SwitchService _switchService;
        private readonly ILogger<SwitchController> _logger;
        private readonly SwitchMonitoringService _monitoringService;

        public SwitchController(SwitchService switchService, ILogger<SwitchController> logger, IServiceProvider serviceProvider)
        {
            _switchService = switchService;
            _logger = logger;
            
            // Get the monitoring service from hosted services
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            _monitoringService = hostedServices.OfType<SwitchMonitoringService>().FirstOrDefault()
                ?? throw new InvalidOperationException("SwitchMonitoringService not found");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            _logger.LogInformation("Login attempt from {ClientIP} to switch {SwitchHost} with username {Username}", 
                clientIp, request.Host, request.Username);
            
            try
            {
                await _switchService.EnsureClientAsync(request);
                stopwatch.Stop();
                
                _logger.LogInformation("Login successful for {Username}@{SwitchHost} from {ClientIP} in {ElapsedMs}ms", 
                    request.Username, request.Host, clientIp, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Login successful" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Login failed for {Username}@{SwitchHost} from {ClientIP} after {ElapsedMs}ms: {ErrorMessage}", 
                    request.Username, request.Host, clientIp, stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // This will test if we can connect and get basic info
                var systemInfo = await _switchService.GetSystemInfoAsync();
                return Ok(new { 
                    status = "healthy", 
                    connected = true, 
                    deviceName = systemInfo.DeviceName,
                    ipAddress = systemInfo.IpAddress,
                    firmwareVersion = systemInfo.FirmwareVersion,
                    message = "Successfully connected to switch",
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    status = "unhealthy", 
                    connected = false, 
                    message = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] LoginRequest request)
        {
            try
            {
                // Just test basic connectivity without saving credentials
                using var client = new Services.TplinkClient($"http://{request.Host}", request.Username, request.Password);
                var canConnect = await client.TestConnectionAsync();
                
                if (!canConnect)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Cannot reach switch at {request.Host}. Please check the IP address and network connectivity." 
                    });
                }

                return Ok(new { 
                    success = true, 
                    message = "Connection test successful" 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Connection test failed: {ex.Message}" 
                });
            }
        }

        [HttpGet("systeminfo")]
        public async Task<IActionResult> GetSystemInfo()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Getting system information");
            
            try
            {
                var systemInfo = await _switchService.GetSystemInfoAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Retrieved system info for device {DeviceName} ({IpAddress}) in {ElapsedMs}ms", 
                    systemInfo.DeviceName, systemInfo.IpAddress, stopwatch.ElapsedMilliseconds);
                
                return Ok(systemInfo);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to get system information after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("ports")]
        public async Task<IActionResult> GetPortInfo()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Getting port information");
            
            try
            {
                var portInfo = await _switchService.GetPortInfoAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Retrieved port info for {PortCount} ports in {ElapsedMs}ms", 
                    portInfo.Ports?.Count ?? 0, stopwatch.ElapsedMilliseconds);
                
                return Ok(portInfo);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to get port information after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("ports/configure")]
        public async Task<IActionResult> ConfigurePort([FromBody] PortConfigRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Configuring port {Port}: Enable={Enable}, Speed={Speed}, FlowControl={FlowControl}", 
                request.Port, request.Enable, request.Speed, request.FlowControl);
            
            try
            {
                await _switchService.SetPortConfigAsync(request.Port, request.Enable, request.Speed, request.FlowControl);
                stopwatch.Stop();
                
                _logger.LogInformation("Port {Port} configured successfully in {ElapsedMs}ms", 
                    request.Port, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"Port {request.Port} configured successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to configure port {Port} after {ElapsedMs}ms: {ErrorMessage}", 
                    request.Port, stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("vlans")]
        public async Task<IActionResult> GetVlanConfiguration()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Getting VLAN configuration");
            
            try
            {
                var vlanConfig = await _switchService.GetVlanConfigurationAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Retrieved VLAN config with {VlanCount} VLANs in {ElapsedMs}ms", 
                    vlanConfig.Vlans?.Count ?? 0, stopwatch.ElapsedMilliseconds);
                
                return Ok(vlanConfig);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to get VLAN configuration after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("vlans/create")]
        public async Task<IActionResult> CreateVlan([FromBody] CreateVlanRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Support both legacy (Ports) and new (TaggedPorts/UntaggedPorts) request formats
            var taggedPorts = request.TaggedPorts?.Length > 0 ? request.TaggedPorts : Array.Empty<int>();
            var untaggedPorts = request.UntaggedPorts?.Length > 0 ? request.UntaggedPorts : request.Ports ?? Array.Empty<int>();
            var allPorts = taggedPorts.Concat(untaggedPorts).ToArray();
            
            _logger.LogInformation("Creating VLAN {VlanId} ({VlanName}) with tagged ports: [{TaggedPorts}] and untagged ports: [{UntaggedPorts}]", 
                request.VlanId, request.VlanName, string.Join(", ", taggedPorts), string.Join(", ", untaggedPorts));
                
            try
            {
                await _switchService.CreateVlanAsync(request.VlanId, request.VlanName, taggedPorts, untaggedPorts);
                stopwatch.Stop();
                
                _logger.LogInformation("VLAN {VlanId} ({VlanName}) created successfully in {ElapsedMs}ms", 
                    request.VlanId, request.VlanName, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"VLAN {request.VlanId} ({request.VlanName}) created successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to create VLAN {VlanId} after {ElapsedMs}ms: {ErrorMessage}", 
                    request.VlanId, stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("vlans/delete")]
        public async Task<IActionResult> DeleteVlans([FromBody] DeleteVlanRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Deleting VLANs: {VlanIds}", string.Join(", ", request.VlanIds));
                
            try
            {
                await _switchService.DeleteVlansAsync(request.VlanIds);
                stopwatch.Stop();
                
                _logger.LogInformation("VLANs {VlanIds} deleted successfully in {ElapsedMs}ms", 
                    string.Join(", ", request.VlanIds), stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"VLAN(s) {string.Join(",", request.VlanIds)} deleted successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to delete VLANs {VlanIds} after {ElapsedMs}ms: {ErrorMessage}", 
                    string.Join(", ", request.VlanIds), stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("vlans/pvid")]
        public async Task<IActionResult> SetPvid([FromBody] SetPvidRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting PVID {Pvid} for ports: {Ports}", 
                request.Pvid, string.Join(", ", request.Ports));
                
            try
            {
                await _switchService.SetPvidAsync(request.Ports, request.Pvid);
                stopwatch.Stop();
                
                _logger.LogInformation("PVID {Pvid} set successfully for ports {Ports} in {ElapsedMs}ms", 
                    request.Pvid, string.Join(", ", request.Ports), stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"PVID {request.Pvid} set successfully for ports {string.Join(",", request.Ports)}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set PVID {Pvid} for ports {Ports} after {ElapsedMs}ms: {ErrorMessage}", 
                    request.Pvid, string.Join(", ", request.Ports), stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("diagnostics/cable")]
        public async Task<IActionResult> RunCableDiagnostics([FromBody] CableDiagnosticRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Running cable diagnostics for ports: {Ports}", string.Join(", ", request.Ports));
                
            try
            {
                var diagnostics = await _switchService.RunCableDiagnosticsAsync(request.Ports);
                stopwatch.Stop();
                
                _logger.LogInformation("Cable diagnostics completed for {PortCount} ports in {ElapsedMs}ms", 
                    request.Ports.Length, stopwatch.ElapsedMilliseconds);
                
                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to run cable diagnostics for ports {Ports} after {ElapsedMs}ms: {ErrorMessage}", 
                    string.Join(", ", request.Ports), stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("diagnostics/cable/port/{port}")]
        public async Task<IActionResult> RunSinglePortDiagnostic(int port)
        {
            try
            {
                var diagnostics = await _switchService.RunCableDiagnosticsAsync(new[] { port });
                return Ok(diagnostics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("reboot")]
        public async Task<IActionResult> RebootSwitch([FromBody] RebootRequest? request = null)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var saveConfig = request?.SaveConfig ?? false;
            _logger.LogWarning("Switch reboot initiated by {ClientIP} (Save config: {SaveConfig})", clientIp, saveConfig);
                
            try
            {
                await _switchService.RebootSwitchAsync(saveConfig);
                _logger.LogWarning("Switch reboot command sent successfully from {ClientIP}", clientIp);
                
                return Ok(new { success = true, message = "Switch reboot initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reboot switch from {ClientIP}: {ErrorMessage}", clientIp, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // System Management Endpoints

        [HttpPost("system/name")]
        public async Task<IActionResult> SetSystemName([FromBody] SystemNameRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting system name to: {SystemName}", request.SystemName);
                
            try
            {
                await _switchService.SetSystemNameAsync(request.SystemName);
                stopwatch.Stop();
                
                _logger.LogInformation("System name set successfully to {SystemName} in {ElapsedMs}ms", 
                    request.SystemName, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"System name set to '{request.SystemName}'" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set system name after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("system/ip-config")]
        public async Task<IActionResult> SetIpConfiguration([FromBody] IpConfigRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting IP configuration: DHCP={DhcpEnabled}, IP={IpAddress}, Mask={SubnetMask}, Gateway={Gateway}", 
                request.DhcpEnabled, request.IpAddress, request.SubnetMask, request.Gateway);
                
            try
            {
                await _switchService.SetIpConfigurationAsync(request.DhcpEnabled, request.IpAddress, request.SubnetMask, request.Gateway);
                stopwatch.Stop();
                
                _logger.LogInformation("IP configuration set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "IP configuration updated successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set IP configuration after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("system/factory-reset")]
        public async Task<IActionResult> FactoryReset()
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogWarning("Factory reset initiated by {ClientIP}", clientIp);
                
            try
            {
                await _switchService.FactoryResetAsync();
                _logger.LogWarning("Factory reset command sent successfully from {ClientIP}", clientIp);
                
                return Ok(new { success = true, message = "Factory reset initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate factory reset from {ClientIP}: {ErrorMessage}", clientIp, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("system/save-config")]
        public async Task<IActionResult> SaveConfiguration()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Saving configuration");
                
            try
            {
                await _switchService.SaveConfigurationAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Configuration saved successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Configuration saved successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to save configuration after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("system/led-control")]
        public async Task<IActionResult> SetLedControl([FromBody] LedControlRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting LED control: {LedEnabled}", request.LedEnabled);
                
            try
            {
                await _switchService.SetLedControlAsync(request.LedEnabled);
                stopwatch.Stop();
                
                _logger.LogInformation("LED control set successfully to {LedEnabled} in {ElapsedMs}ms", 
                    request.LedEnabled, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"LED {(request.LedEnabled ? "enabled" : "disabled")}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set LED control after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("system/user-account")]
        public async Task<IActionResult> SetUserAccount([FromBody] UserAccountRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting user account for: {NewUsername}", request.NewUsername);
                
            try
            {
                await _switchService.SetUserAccountAsync(request.NewUsername, request.CurrentPassword, request.NewPassword, request.ConfirmPassword);
                stopwatch.Stop();
                
                _logger.LogInformation("User account set successfully for {NewUsername} in {ElapsedMs}ms", 
                    request.NewUsername, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "User account updated successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set user account after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Port Management Extensions

        [HttpPost("ports/clear-statistics")]
        public async Task<IActionResult> ClearPortStatistics()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Clearing port statistics");
                
            try
            {
                await _switchService.ClearPortStatisticsAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Port statistics cleared successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Port statistics cleared successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to clear port statistics after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Advanced Features Endpoints

        [HttpPost("mirroring/enable")]
        public async Task<IActionResult> SetPortMirroringEnabled([FromBody] PortMirrorEnableRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting port mirroring: {Enabled}, Destination Port: {DestinationPort}", 
                request.Enabled, request.MirrorDestinationPort);
                
            try
            {
                await _switchService.SetPortMirroringEnabledAsync(request.Enabled, request.MirrorDestinationPort);
                stopwatch.Stop();
                
                _logger.LogInformation("Port mirroring set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"Port mirroring {(request.Enabled ? "enabled" : "disabled")}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set port mirroring after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("mirroring/configure")]
        public async Task<IActionResult> ConfigurePortMirroring([FromBody] PortMirrorConfigRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Configuring port mirroring: Source Ports: {SourcePorts}, Ingress: {IngressEnabled}, Egress: {EgressEnabled}", 
                string.Join(",", request.SourcePorts), request.IngressEnabled, request.EgressEnabled);
                
            try
            {
                await _switchService.ConfigurePortMirroringAsync(request.SourcePorts, request.IngressEnabled, request.EgressEnabled);
                stopwatch.Stop();
                
                _logger.LogInformation("Port mirroring configured successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Port mirroring configured successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to configure port mirroring after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("trunking/configure")]
        public async Task<IActionResult> SetPortTrunking([FromBody] PortTrunkRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting port trunking: Trunk ID: {TrunkId}, Member Ports: {MemberPorts}", 
                request.TrunkId, string.Join(",", request.MemberPorts));
                
            try
            {
                await _switchService.SetPortTrunkingAsync(request.TrunkId, request.MemberPorts);
                stopwatch.Stop();
                
                _logger.LogInformation("Port trunking configured successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"Port trunking configured for LAG {request.TrunkId}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to configure port trunking after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("loop-prevention")]
        public async Task<IActionResult> SetLoopPrevention([FromBody] LoopPreventionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting loop prevention: {Enabled}", request.Enabled);
                
            try
            {
                await _switchService.SetLoopPreventionAsync(request.Enabled);
                stopwatch.Stop();
                
                _logger.LogInformation("Loop prevention set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"Loop prevention {(request.Enabled ? "enabled" : "disabled")}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set loop prevention after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // QoS Endpoints

        [HttpPost("qos/mode")]
        public async Task<IActionResult> SetQosMode([FromBody] QosModeRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting QoS mode: {Mode}", request.Mode);
                
            try
            {
                await _switchService.SetQosModeAsync(request.Mode);
                stopwatch.Stop();
                
                _logger.LogInformation("QoS mode set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"QoS mode set to {request.Mode}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set QoS mode after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("qos/bandwidth-control")]
        public async Task<IActionResult> SetBandwidthControl([FromBody] BandwidthControlRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting bandwidth control: Ports: {Ports}, Ingress: {IngressRate} Kbps, Egress: {EgressRate} Kbps", 
                string.Join(",", request.Ports), request.IngressRate, request.EgressRate);
                
            try
            {
                await _switchService.SetBandwidthControlAsync(request.Ports, request.IngressRate, request.EgressRate);
                stopwatch.Stop();
                
                _logger.LogInformation("Bandwidth control set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Bandwidth control configured successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set bandwidth control after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("qos/port-priority")]
        public async Task<IActionResult> SetPortPriority([FromBody] PortPriorityRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting port priority: Ports: {Ports}, Priority: {Priority}", 
                string.Join(",", request.Ports), request.Priority);
                
            try
            {
                await _switchService.SetPortPriorityAsync(request.Ports, request.Priority);
                stopwatch.Stop();
                
                _logger.LogInformation("Port priority set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"Port priority set to {request.Priority}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set port priority after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("qos/storm-control")]
        public async Task<IActionResult> SetStormControl([FromBody] StormControlRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting storm control: Ports: {Ports}, Broadcast: {BroadcastRate}, Multicast: {MulticastRate}, Unicast: {UnicastRate}", 
                string.Join(",", request.Ports), request.BroadcastRate, request.MulticastRate, request.UnicastRate);
                
            try
            {
                await _switchService.SetStormControlAsync(request.Ports, request.BroadcastRate, request.MulticastRate, request.UnicastRate);
                stopwatch.Stop();
                
                _logger.LogInformation("Storm control set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Storm control configured successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set storm control after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // IGMP Snooping Endpoint

        [HttpPost("igmp-snooping")]
        public async Task<IActionResult> SetIgmpSnooping([FromBody] IgmpSnoopingRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting IGMP snooping: {Enabled}", request.Enabled);
                
            try
            {
                await _switchService.SetIgmpSnoopingAsync(request.Enabled);
                stopwatch.Stop();
                
                _logger.LogInformation("IGMP snooping set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"IGMP snooping {(request.Enabled ? "enabled" : "disabled")}" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set IGMP snooping after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PoE Management Endpoints

        [HttpPost("poe/global-config")]
        public async Task<IActionResult> SetPoeGlobalConfig([FromBody] PoeGlobalConfigRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting PoE global config: Power Limit: {PowerLimit}W", request.PowerLimit);
                
            try
            {
                await _switchService.SetPoeGlobalConfigAsync(request.PowerLimit);
                stopwatch.Stop();
                
                _logger.LogInformation("PoE global config set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"PoE power limit set to {request.PowerLimit}W" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set PoE global config after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("poe/port-config")]
        public async Task<IActionResult> SetPoePortConfig([FromBody] PoePortConfigRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Setting PoE port config: Ports: {Ports}, State: {State}, Priority: {Priority}, PowerLimit: {PowerLimit}", 
                string.Join(",", request.Ports), request.State, request.Priority, request.PowerLimit);
                
            try
            {
                await _switchService.SetPoePortConfigAsync(request.Ports, (int)request.State, (int)request.Priority, (int)request.PowerLimit, request.ManualPowerLimit);
                stopwatch.Stop();
                
                _logger.LogInformation("PoE port config set successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "PoE port configuration updated successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to set PoE port config after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Configuration Management Endpoints

        [HttpPost("config/backup")]
        public async Task<IActionResult> BackupConfiguration()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Backing up configuration");
                
            try
            {
                var configStream = await _switchService.BackupConfigurationAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("Configuration backup completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                var fileName = $"switch-config-{DateTime.Now:yyyyMMdd-HHmmss}.cfg";
                return File(configStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to backup configuration after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("config/restore")]
        public async Task<IActionResult> RestoreConfiguration([FromForm] ConfigRestoreRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Restoring configuration from file: {FileName}", request.ConfigFile.FileName);
                
            try
            {
                using var stream = request.ConfigFile.OpenReadStream();
                await _switchService.RestoreConfigurationAsync(stream, request.ConfigFile.FileName);
                stopwatch.Stop();
                
                _logger.LogInformation("Configuration restored successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Configuration restored successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to restore configuration after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("firmware/upgrade")]
        public async Task<IActionResult> UpgradeFirmware([FromForm] FirmwareUpgradeRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogWarning("Firmware upgrade initiated with file: {FileName}", request.FirmwareFile.FileName);
                
            try
            {
                using var stream = request.FirmwareFile.OpenReadStream();
                await _switchService.UpgradeFirmwareAsync(stream, request.FirmwareFile.FileName);
                stopwatch.Stop();
                
                _logger.LogWarning("Firmware upgrade completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = "Firmware upgrade initiated successfully" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to upgrade firmware after {ElapsedMs}ms: {ErrorMessage}", 
                    stopwatch.ElapsedMilliseconds, ex.Message);
                
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("monitoring/status")]
        public IActionResult GetMonitoringStatus()
        {
            try
            {
                var status = new
                {
                    isRunning = true,
                    isAuthenticated = _monitoringService.IsAuthenticated,
                    lastSuccessfulConnection = _monitoringService.LastSuccessfulConnection,
                    lastCookieRenewal = _monitoringService.LastCookieRenewal
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get monitoring status");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}