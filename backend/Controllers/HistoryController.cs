using Microsoft.AspNetCore.Mvc;
using TPLinkWebUI.Services;
using TPLinkWebUI.Models.History;

namespace TPLinkWebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly HistoryService _historyService;
        private readonly AnalyticsService _analyticsService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(HistoryService historyService, AnalyticsService analyticsService, ILogger<HistoryController> logger)
        {
            _historyService = historyService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        [HttpGet("ports")]
        public async Task<ActionResult<List<PortHistoryEntry>>> GetPortHistory(
            [FromQuery] int? portNumber = null,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetPortHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port history");
                return StatusCode(500, "Failed to retrieve port history");
            }
        }

        [HttpGet("ports/{portNumber}")]
        public async Task<ActionResult<List<PortHistoryEntry>>> GetPortHistoryByPort(
            int portNumber,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetPortHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port history for port {PortNumber}", portNumber);
                return StatusCode(500, $"Failed to retrieve port history for port {portNumber}");
            }
        }

        [HttpGet("cable-diagnostics")]
        public async Task<ActionResult<List<CableDiagnosticHistoryEntry>>> GetCableDiagnosticHistory(
            [FromQuery] int? portNumber = null,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetCableDiagnosticHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cable diagnostic history");
                return StatusCode(500, "Failed to retrieve cable diagnostic history");
            }
        }

        [HttpGet("cable-diagnostics/{portNumber}")]
        public async Task<ActionResult<List<CableDiagnosticHistoryEntry>>> GetCableDiagnosticHistoryByPort(
            int portNumber,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetCableDiagnosticHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cable diagnostic history for port {PortNumber}", portNumber);
                return StatusCode(500, $"Failed to retrieve cable diagnostic history for port {portNumber}");
            }
        }

        [HttpGet("system-info")]
        public async Task<ActionResult<List<SystemInfoHistoryEntry>>> GetSystemInfoHistory(
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetSystemInfoHistoryAsync(since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system info history");
                return StatusCode(500, "Failed to retrieve system info history");
            }
        }

        [HttpGet("vlans")]
        public async Task<ActionResult<List<VlanHistoryEntry>>> GetVlanHistory(
            [FromQuery] int? vlanId = null,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetVlanHistoryAsync(vlanId, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get VLAN history");
                return StatusCode(500, "Failed to retrieve VLAN history");
            }
        }

        [HttpGet("vlans/{vlanId}")]
        public async Task<ActionResult<List<VlanHistoryEntry>>> GetVlanHistoryById(
            int vlanId,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetVlanHistoryAsync(vlanId, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get VLAN history for VLAN {VlanId}", vlanId);
                return StatusCode(500, $"Failed to retrieve VLAN history for VLAN {vlanId}");
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetHistorySummary([FromQuery] DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);
                
                var portHistory = await _historyService.GetPortHistoryAsync(null, sinceDate, 1000);
                var cableHistory = await _historyService.GetCableDiagnosticHistoryAsync(null, sinceDate, 1000);
                var systemHistory = await _historyService.GetSystemInfoHistoryAsync(sinceDate, 1000);
                var vlanHistory = await _historyService.GetVlanHistoryAsync(null, sinceDate, 1000);
                var connectivityHistory = await _historyService.GetSwitchConnectivityHistoryAsync(sinceDate, 1000);
                var userActivity = await _historyService.GetUserActivityHistoryAsync(null, null, sinceDate, 1000);

                var summary = new
                {
                    Period = new { Since = sinceDate, Until = DateTime.UtcNow },
                    Counts = new
                    {
                        PortChanges = portHistory.Count,
                        CableTests = cableHistory.Count,
                        SystemUpdates = systemHistory.Count,
                        VlanChanges = vlanHistory.Count,
                        ConnectivityEvents = connectivityHistory.Count,
                        UserActions = userActivity.Count
                    },
                    RecentActivity = new
                    {
                        PortChanges = portHistory.Where(p => p.ChangeType != "PERIODIC_SNAPSHOT").Take(10),
                        CableTests = cableHistory.Where(c => c.TestTrigger == "MANUAL").Take(10),
                        VlanChanges = vlanHistory.Where(v => v.ChangeType != "PERIODIC_SNAPSHOT").Take(10),
                        ConnectivityChanges = connectivityHistory.Take(10),
                        UserActions = userActivity.Take(10)
                    },
                    PortStats = portHistory
                        .GroupBy(p => p.PortNumber)
                        .Select(g => new { PortNumber = g.Key, ChangeCount = g.Count() })
                        .OrderByDescending(s => s.ChangeCount)
                        .Take(10)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get history summary");
                return StatusCode(500, "Failed to retrieve history summary");
            }
        }

        [HttpGet("switch-connectivity")]
        public async Task<ActionResult<List<SwitchConnectivityHistoryEntry>>> GetSwitchConnectivityHistory(
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetSwitchConnectivityHistoryAsync(since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get switch connectivity history");
                return StatusCode(500, "Failed to retrieve switch connectivity history");
            }
        }

        [HttpGet("port-statistics")]
        public async Task<ActionResult<List<PortStatisticsHistoryEntry>>> GetPortStatisticsHistory(
            [FromQuery] int? portNumber = null,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetPortStatisticsHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port statistics history");
                return StatusCode(500, "Failed to retrieve port statistics history");
            }
        }

        [HttpGet("port-statistics/{portNumber}")]
        public async Task<ActionResult<List<PortStatisticsHistoryEntry>>> GetPortStatisticsHistoryByPort(
            int portNumber,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetPortStatisticsHistoryAsync(portNumber, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port statistics history for port {PortNumber}", portNumber);
                return StatusCode(500, $"Failed to retrieve port statistics history for port {portNumber}");
            }
        }

        [HttpGet("user-activity")]
        public async Task<ActionResult<List<UserActivityHistoryEntry>>> GetUserActivityHistory(
            [FromQuery] int? userId = null,
            [FromQuery] string? username = null,
            [FromQuery] DateTime? since = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var history = await _historyService.GetUserActivityHistoryAsync(userId, username, since, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user activity history");
                return StatusCode(500, "Failed to retrieve user activity history");
            }
        }

        [HttpGet("analytics")]
        public async Task<ActionResult<NetworkHealthAnalysis>> GetNetworkAnalytics(
            [FromQuery] DateTime? since = null)
        {
            try
            {
                var analysis = await _analyticsService.GetNetworkHealthAnalysisAsync(since);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get network analytics");
                return StatusCode(500, "Failed to retrieve network analytics");
            }
        }

        [HttpGet("recommendations")]
        public async Task<ActionResult<List<Recommendation>>> GetRecommendations()
        {
            try
            {
                var recommendations = await _analyticsService.GetRecommendationsAsync();
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recommendations");
                return StatusCode(500, "Failed to retrieve recommendations");
            }
        }

        [HttpGet("port-analytics/{portNumber}")]
        public async Task<ActionResult<PortAnalytics>> GetPortAnalytics(
            int portNumber,
            [FromQuery] DateTime? since = null)
        {
            try
            {
                var analytics = await _analyticsService.GetPortAnalyticsAsync(portNumber, since);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port analytics for port {PortNumber}", portNumber);
                return StatusCode(500, $"Failed to retrieve port analytics for port {portNumber}");
            }
        }
    }
}