using Microsoft.EntityFrameworkCore;
using TPLinkWebUI.Data;
using TPLinkWebUI.Models;
using TPLinkWebUI.Models.History;

namespace TPLinkWebUI.Services
{
    public class HistoryService
    {
        private readonly SwitchHistoryContext _context;
        private readonly ILogger<HistoryService> _logger;

        public HistoryService(SwitchHistoryContext context, ILogger<HistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Port History Methods
        public async Task LogPortInfoAsync(List<PortInfo> currentPorts, List<PortInfo>? previousPorts = null, string changeType = "PERIODIC_SNAPSHOT", int? userId = null, string? username = null)
        {
            try
            {
                var entries = new List<PortHistoryEntry>();

                foreach (var port in currentPorts)
                {
                    var previousPort = previousPorts?.FirstOrDefault(p => p.PortNumber == port.PortNumber);
                    string? previousValue = null;
                    string? newValue = null;
                    TimeSpan? downtimeDuration = null;
                    DateTime? lastUpTime = null;

                    if (previousPort != null && (changeType == "STATUS_CHANGE" || changeType == "CONFIG_CHANGE"))
                    {
                        var changes = GetPortChanges(previousPort, port);
                        if (changes.Any())
                        {
                            previousValue = string.Join(", ", changes.Select(c => $"{c.Field}: {c.OldValue}"));
                            newValue = string.Join(", ", changes.Select(c => $"{c.Field}: {c.NewValue}"));
                            
                            // Calculate downtime if port is coming back up
                            if (!previousPort.IsConnected && port.IsConnected)
                            {
                                var lastEntry = await _context.PortHistory
                                    .Where(h => h.PortNumber == port.PortNumber && !h.IsConnected)
                                    .OrderByDescending(h => h.Timestamp)
                                    .FirstOrDefaultAsync();
                                
                                if (lastEntry != null)
                                {
                                    downtimeDuration = DateTime.UtcNow - lastEntry.Timestamp;
                                }
                            }
                        }
                    }

                    string? notes = null;
                    if (downtimeDuration.HasValue)
                    {
                        var duration = downtimeDuration.Value;
                        string formattedDuration;
                        if (duration.TotalDays >= 1)
                            formattedDuration = $"{(int)duration.TotalDays} days, {duration.Hours} hours";
                        else if (duration.TotalHours >= 1)
                            formattedDuration = $"{(int)duration.TotalHours} hours, {duration.Minutes} minutes";
                        else
                            formattedDuration = $"{(int)duration.TotalMinutes} minutes";
                        
                        notes = $"Port {port.PortNumber} back up after {formattedDuration}";
                    }

                    entries.Add(PortHistoryEntry.FromPortInfo(port, changeType, previousValue, newValue, notes, userId, username, downtimeDuration, lastUpTime));
                }

                _context.PortHistory.AddRange(entries);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged {Count} port history entries with change type: {ChangeType}", entries.Count, changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log port history");
            }
        }

        public async Task LogPortConfigChangeAsync(int portNumber, PortInfo oldConfig, PortInfo newConfig, int? userId = null, string? username = null)
        {
            try
            {
                var changes = GetPortChanges(oldConfig, newConfig);
                if (!changes.Any()) return;

                var previousValue = string.Join(", ", changes.Select(c => $"{c.Field}: {c.OldValue}"));
                var newValue = string.Join(", ", changes.Select(c => $"{c.Field}: {c.NewValue}"));

                var entry = PortHistoryEntry.FromPortInfo(newConfig, "CONFIG_CHANGE", previousValue, newValue,
                    $"Port {portNumber} configuration changed by {username ?? "System"}", userId, username);

                _context.PortHistory.Add(entry);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged port {PortNumber} configuration change by user {Username}", portNumber, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log port configuration change for port {PortNumber}", portNumber);
            }
        }

        // Cable Diagnostic History Methods
        public async Task LogCableDiagnosticsAsync(List<PortDiagnosticInfo> diagnostics, string testTrigger = "MANUAL")
        {
            try
            {
                var entries = diagnostics.Select(d => CableDiagnosticHistoryEntry.FromPortDiagnosticInfo(d, testTrigger)).ToList();
                
                _context.CableDiagnosticHistory.AddRange(entries);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged {Count} cable diagnostic entries with trigger: {TestTrigger}", entries.Count, testTrigger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log cable diagnostics");
            }
        }

        // System Info History Methods
        public async Task LogSystemInfoAsync(SystemInfoResponse systemInfo, string changeType = "PERIODIC_SNAPSHOT")
        {
            try
            {
                var entry = SystemInfoHistoryEntry.FromSystemInfoResponse(systemInfo, changeType);
                
                _context.SystemInfoHistory.Add(entry);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged system info with change type: {ChangeType}", changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system info");
            }
        }

        // VLAN History Methods
        public async Task LogVlanInfoAsync(List<VlanInfo> vlans, string changeType = "PERIODIC_SNAPSHOT")
        {
            try
            {
                var entries = vlans.Select(v => VlanHistoryEntry.FromVlanInfo(v, changeType)).ToList();
                
                _context.VlanHistory.AddRange(entries);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged {Count} VLAN history entries with change type: {ChangeType}", entries.Count, changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log VLAN info");
            }
        }

        public async Task LogVlanChangeAsync(VlanInfo vlan, string changeType, string? notes = null)
        {
            try
            {
                var entry = VlanHistoryEntry.FromVlanInfo(vlan, changeType, notes: notes);
                
                _context.VlanHistory.Add(entry);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged VLAN {VlanId} change: {ChangeType}", vlan.VlanId, changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log VLAN change for VLAN {VlanId}", vlan.VlanId);
            }
        }

        // Query Methods
        public async Task<List<PortHistoryEntry>> GetPortHistoryAsync(int? portNumber = null, DateTime? since = null, int limit = 100)
        {
            var query = _context.PortHistory.AsQueryable();

            if (portNumber.HasValue)
                query = query.Where(h => h.PortNumber == portNumber.Value);

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        // Switch Connectivity History Methods
        public async Task LogSwitchConnectivityAsync(bool isReachable, string? ipAddress, int? responseTimeMs = null, string? errorMessage = null)
        {
            try
            {
                TimeSpan? downtimeDuration = null;
                
                if (isReachable)
                {
                    // Check if switch was previously unreachable and calculate downtime
                    var lastUnreachableEntry = await _context.SwitchConnectivityHistory
                        .Where(h => !h.IsReachable)
                        .OrderByDescending(h => h.Timestamp)
                        .FirstOrDefaultAsync();
                    
                    if (lastUnreachableEntry != null)
                    {
                        downtimeDuration = DateTime.UtcNow - lastUnreachableEntry.Timestamp;
                    }
                }
                
                var entry = isReachable 
                    ? SwitchConnectivityHistoryEntry.CreateReachable(ipAddress, responseTimeMs)
                    : SwitchConnectivityHistoryEntry.CreateUnreachable(ipAddress, errorMessage, downtimeDuration);

                _context.SwitchConnectivityHistory.Add(entry);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged switch connectivity status: {Status}", isReachable ? "Reachable" : "Unreachable");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log switch connectivity");
            }
        }

        public async Task<List<SwitchConnectivityHistoryEntry>> GetSwitchConnectivityHistoryAsync(DateTime? since = null, int limit = 100)
        {
            var query = _context.SwitchConnectivityHistory.AsQueryable();

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        // Port Statistics History Methods
        public async Task LogPortStatisticsAsync(Dictionary<int, PortStatistics> portStatistics, string changeType = "STATISTICS_UPDATE")
        {
            try
            {
                var entries = portStatistics.Select(kvp => 
                    PortStatisticsHistoryEntry.FromPortStatistics(kvp.Key, kvp.Value, changeType)
                ).ToList();

                _context.PortStatisticsHistory.AddRange(entries);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged statistics for {Count} ports", entries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log port statistics");
            }
        }

        public async Task<List<PortStatisticsHistoryEntry>> GetPortStatisticsHistoryAsync(int? portNumber = null, DateTime? since = null, int limit = 100)
        {
            var query = _context.PortStatisticsHistory.AsQueryable();

            if (portNumber.HasValue)
                query = query.Where(h => h.PortNumber == portNumber.Value);

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        // User Activity History Methods
        public async Task LogUserActivityAsync(UserActivityHistoryEntry activity)
        {
            try
            {
                _context.UserActivityHistory.Add(activity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Logged user activity: {ActionType} by {Username}", activity.ActionType, activity.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user activity");
            }
        }

        public async Task<List<UserActivityHistoryEntry>> GetUserActivityHistoryAsync(int? userId = null, string? username = null, DateTime? since = null, int limit = 100)
        {
            var query = _context.UserActivityHistory.AsQueryable();

            if (userId.HasValue)
                query = query.Where(h => h.UserId == userId.Value);
            
            if (!string.IsNullOrEmpty(username))
                query = query.Where(h => h.Username == username);

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<CableDiagnosticHistoryEntry>> GetCableDiagnosticHistoryAsync(int? portNumber = null, DateTime? since = null, int limit = 100)
        {
            var query = _context.CableDiagnosticHistory.AsQueryable();

            if (portNumber.HasValue)
                query = query.Where(h => h.PortNumber == portNumber.Value);

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<SystemInfoHistoryEntry>> GetSystemInfoHistoryAsync(DateTime? since = null, int limit = 100)
        {
            var query = _context.SystemInfoHistory.AsQueryable();

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<VlanHistoryEntry>> GetVlanHistoryAsync(int? vlanId = null, DateTime? since = null, int limit = 100)
        {
            var query = _context.VlanHistory.AsQueryable();

            if (vlanId.HasValue)
                query = query.Where(h => h.VlanId == vlanId.Value);

            if (since.HasValue)
                query = query.Where(h => h.Timestamp >= since.Value);

            return await query
                .OrderByDescending(h => h.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        // Helper Methods
        private List<FieldChange> GetPortChanges(PortInfo oldPort, PortInfo newPort)
        {
            var changes = new List<FieldChange>();

            if (oldPort.Status != newPort.Status)
                changes.Add(new FieldChange("Status", oldPort.Status, newPort.Status));

            if (oldPort.SpeedConfig != newPort.SpeedConfig)
                changes.Add(new FieldChange("SpeedConfig", oldPort.SpeedConfig, newPort.SpeedConfig));

            if (oldPort.SpeedActual != newPort.SpeedActual)
                changes.Add(new FieldChange("SpeedActual", oldPort.SpeedActual, newPort.SpeedActual));

            if (oldPort.FlowControlConfig != newPort.FlowControlConfig)
                changes.Add(new FieldChange("FlowControlConfig", oldPort.FlowControlConfig, newPort.FlowControlConfig));

            if (oldPort.FlowControlActual != newPort.FlowControlActual)
                changes.Add(new FieldChange("FlowControlActual", oldPort.FlowControlActual, newPort.FlowControlActual));

            if (oldPort.Trunk != newPort.Trunk)
                changes.Add(new FieldChange("Trunk", oldPort.Trunk, newPort.Trunk));

            return changes;
        }

        private record FieldChange(string Field, string OldValue, string NewValue);
    }
}