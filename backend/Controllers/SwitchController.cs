using Microsoft.AspNetCore.Mvc;
using TPLinkWebUI.Models;
using TPLinkWebUI.Services;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using TPLinkWebUI.Configuration;

namespace TPLinkWebUI.Controllers
{
    [ApiController]
    [Route("api")]
    public class SwitchController : ControllerBase
    {
        private readonly SwitchService _switchService;
        private readonly ILogger<SwitchController> _logger;
        private readonly SwitchMonitoringService _monitoringService;
        private readonly IServiceProvider _serviceProvider;

        public SwitchController(SwitchService switchService, ILogger<SwitchController> logger, IServiceProvider serviceProvider)
        {
            _switchService = switchService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            
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
                // Create logger for TplinkClient
                var clientLogger = _serviceProvider.GetRequiredService<ILogger<TplinkClient>>();
                var switchConfig = _serviceProvider.GetRequiredService<IOptions<SwitchConfiguration>>().Value;
                
                using var client = new Services.TplinkClient($"http://{request.Host}", request.Username, request.Password, clientLogger, switchConfig);
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
            _logger.LogInformation("Creating VLAN {VlanId} with ports: {Ports}", 
                request.VlanId, string.Join(", ", request.Ports));
                
            try
            {
                await _switchService.CreateVlanAsync(request.VlanId, request.Ports);
                stopwatch.Stop();
                
                _logger.LogInformation("VLAN {VlanId} created successfully in {ElapsedMs}ms", 
                    request.VlanId, stopwatch.ElapsedMilliseconds);
                
                return Ok(new { success = true, message = $"VLAN {request.VlanId} created successfully" });
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
        public async Task<IActionResult> RebootSwitch()
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogWarning("Switch reboot initiated by {ClientIP}", clientIp);
                
            try
            {
                await _switchService.RebootSwitchAsync();
                _logger.LogWarning("Switch reboot command sent successfully from {ClientIP}", clientIp);
                
                return Ok(new { success = true, message = "Switch reboot initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reboot switch from {ClientIP}: {ErrorMessage}", clientIp, ex.Message);
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