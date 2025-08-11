using Microsoft.EntityFrameworkCore;
using TPLinkWebUI.Data;
using TPLinkWebUI.Models.History;

namespace TPLinkWebUI.Services
{
    public class AnalyticsService
    {
        private readonly SwitchHistoryContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(SwitchHistoryContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<NetworkHealthAnalysis> GetNetworkHealthAnalysisAsync(DateTime? since = null)
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);
            
            try
            {
                var portHistory = await _context.PortHistory
                    .Where(h => h.Timestamp >= sinceDate)
                    .ToListAsync();

                var connectivityHistory = await _context.SwitchConnectivityHistory
                    .Where(h => h.Timestamp >= sinceDate)
                    .ToListAsync();

                var userActivity = await _context.UserActivityHistory
                    .Where(h => h.Timestamp >= sinceDate)
                    .ToListAsync();

                return new NetworkHealthAnalysis
                {
                    AnalysisDate = DateTime.UtcNow,
                    PeriodStart = sinceDate,
                    PeriodEnd = DateTime.UtcNow,
                    OverallHealthScore = CalculateOverallHealthScore(portHistory, connectivityHistory),
                    PortHealthScores = CalculatePortHealthScores(portHistory),
                    ConnectivityReliability = CalculateConnectivityReliability(connectivityHistory),
                    UserActivitySummary = AnalyzeUserActivity(userActivity),
                    Recommendations = GenerateRecommendations(portHistory, connectivityHistory, userActivity)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate network health analysis");
                throw;
            }
        }

        public async Task<List<Recommendation>> GetRecommendationsAsync()
        {
            try
            {
                var analysis = await GetNetworkHealthAnalysisAsync();
                return analysis.Recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recommendations");
                return new List<Recommendation>();
            }
        }

        public async Task<PortAnalytics> GetPortAnalyticsAsync(int portNumber, DateTime? since = null)
        {
            var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);
            
            try
            {
                var history = await _context.PortHistory
                    .Where(h => h.PortNumber == portNumber && h.Timestamp >= sinceDate)
                    .OrderBy(h => h.Timestamp)
                    .ToListAsync();

                var statistics = await _context.PortStatisticsHistory
                    .Where(h => h.PortNumber == portNumber && h.Timestamp >= sinceDate)
                    .OrderBy(h => h.Timestamp)
                    .ToListAsync();

                return new PortAnalytics
                {
                    PortNumber = portNumber,
                    AnalysisDate = DateTime.UtcNow,
                    PeriodStart = sinceDate,
                    PeriodEnd = DateTime.UtcNow,
                    UptimePercentage = CalculateUptimePercentage(history),
                    TotalDowntime = CalculateTotalDowntime(history),
                    AverageDowntimeDuration = CalculateAverageDowntimeDuration(history),
                    ErrorRate = CalculateErrorRate(statistics),
                    ActivityLevel = CalculateActivityLevel(history),
                    LastStatusChange = history.LastOrDefault()?.Timestamp,
                    StatusChanges = history.Count(h => h.ChangeType == "STATUS_CHANGE"),
                    ConfigChanges = history.Count(h => h.ChangeType == "CONFIG_CHANGE")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get port analytics for port {PortNumber}", portNumber);
                throw;
            }
        }

        private double CalculateOverallHealthScore(List<PortHistoryEntry> portHistory, List<SwitchConnectivityHistoryEntry> connectivityHistory)
        {
            var portHealthScore = portHistory.Any() ? 
                portHistory.Count(h => h.IsConnected && h.IsEnabled) / (double)portHistory.Count * 100 : 100;
            
            var connectivityScore = connectivityHistory.Any() ?
                connectivityHistory.Count(h => h.IsReachable) / (double)connectivityHistory.Count * 100 : 100;
            
            return (portHealthScore + connectivityScore) / 2;
        }

        private Dictionary<int, double> CalculatePortHealthScores(List<PortHistoryEntry> portHistory)
        {
            return portHistory
                .GroupBy(h => h.PortNumber)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(h => h.IsConnected && h.IsEnabled) / (double)g.Count() * 100
                );
        }

        private double CalculateConnectivityReliability(List<SwitchConnectivityHistoryEntry> connectivityHistory)
        {
            if (!connectivityHistory.Any()) return 100;
            return connectivityHistory.Count(h => h.IsReachable) / (double)connectivityHistory.Count * 100;
        }

        private UserActivitySummary AnalyzeUserActivity(List<UserActivityHistoryEntry> userActivity)
        {
            return new UserActivitySummary
            {
                TotalActions = userActivity.Count,
                ConfigChanges = userActivity.Count(u => u.ActionType == "CONFIG_CHANGE"),
                SystemCommands = userActivity.Count(u => u.ActionType == "SYSTEM_COMMAND"),
                FailedActions = userActivity.Count(u => !u.IsSuccess),
                ActiveUsers = userActivity.Select(u => u.Username).Distinct().Count(),
                MostActiveUser = userActivity.GroupBy(u => u.Username)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key
            };
        }

        private List<Recommendation> GenerateRecommendations(
            List<PortHistoryEntry> portHistory, 
            List<SwitchConnectivityHistoryEntry> connectivityHistory,
            List<UserActivityHistoryEntry> userActivity)
        {
            var recommendations = new List<Recommendation>();

            // Port-based recommendations
            var problematicPorts = portHistory
                .GroupBy(h => h.PortNumber)
                .Where(g => g.Count(h => !h.IsConnected) / (double)g.Count() > 0.1) // More than 10% downtime
                .Select(g => g.Key)
                .ToList();

            foreach (var port in problematicPorts)
            {
                var downEvents = portHistory.Where(h => h.PortNumber == port && !h.IsConnected).Count();
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.PortMaintenance,
                    Priority = RecommendationPriority.High,
                    Title = $"Port {port} Reliability Issue",
                    Description = $"Port {port} has experienced {downEvents} connection issues. Consider checking cable connections or replacing the port.",
                    AffectedEntity = $"Port {port}",
                    Confidence = 0.85
                });
            }

            // Connectivity recommendations
            var connectivityIssues = connectivityHistory.Count(h => !h.IsReachable);
            if (connectivityIssues > 0)
            {
                var reliability = connectivityHistory.Count(h => h.IsReachable) / (double)connectivityHistory.Count * 100;
                if (reliability < 95)
                {
                    recommendations.Add(new Recommendation
                    {
                        Type = RecommendationType.NetworkInfrastructure,
                        Priority = RecommendationPriority.High,
                        Title = "Switch Connectivity Issues",
                        Description = $"Switch connectivity reliability is {reliability:F1}%. Consider checking network infrastructure.",
                        AffectedEntity = "Switch",
                        Confidence = 0.9
                    });
                }
            }

            // User activity recommendations
            var failedActions = userActivity.Count(u => !u.IsSuccess);
            if (failedActions > 5)
            {
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.UserTraining,
                    Priority = RecommendationPriority.Medium,
                    Title = "High Rate of Failed Actions",
                    Description = $"There have been {failedActions} failed user actions. Consider additional training or reviewing procedures.",
                    AffectedEntity = "Users",
                    Confidence = 0.7
                });
            }

            // Configuration change patterns
            var configChanges = userActivity.Where(u => u.ActionType == "CONFIG_CHANGE").ToList();
            var frequentConfigUsers = configChanges
                .GroupBy(u => u.Username)
                .Where(g => g.Count() > 10)
                .Select(g => g.Key)
                .ToList();

            foreach (var user in frequentConfigUsers)
            {
                recommendations.Add(new Recommendation
                {
                    Type = RecommendationType.ProcessImprovement,
                    Priority = RecommendationPriority.Low,
                    Title = $"Frequent Configuration Changes by {user}",
                    Description = $"User {user} has made many configuration changes. Consider automation or standardized procedures.",
                    AffectedEntity = user,
                    Confidence = 0.6
                });
            }

            return recommendations.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Confidence).ToList();
        }

        private double CalculateUptimePercentage(List<PortHistoryEntry> history)
        {
            if (!history.Any()) return 100;
            return history.Count(h => h.IsConnected && h.IsEnabled) / (double)history.Count * 100;
        }

        private TimeSpan CalculateTotalDowntime(List<PortHistoryEntry> history)
        {
            return TimeSpan.FromMinutes(history.Where(h => h.DowntimeDuration.HasValue)
                .Sum(h => h.DowntimeDuration!.Value.TotalMinutes));
        }

        private TimeSpan? CalculateAverageDowntimeDuration(List<PortHistoryEntry> history)
        {
            var downtimes = history.Where(h => h.DowntimeDuration.HasValue).ToList();
            if (!downtimes.Any()) return null;
            
            var averageMinutes = downtimes.Average(h => h.DowntimeDuration!.Value.TotalMinutes);
            return TimeSpan.FromMinutes(averageMinutes);
        }

        private double CalculateErrorRate(List<PortStatisticsHistoryEntry> statistics)
        {
            if (!statistics.Any()) return 0;
            var latest = statistics.LastOrDefault();
            return latest?.TxErrorRate ?? 0;
        }

        private string CalculateActivityLevel(List<PortHistoryEntry> history)
        {
            var changeCount = history.Count;
            return changeCount switch
            {
                > 50 => "Very High",
                > 20 => "High", 
                > 10 => "Medium",
                > 0 => "Low",
                _ => "None"
            };
        }
    }

    // Data models for analytics
    public class NetworkHealthAnalysis
    {
        public DateTime AnalysisDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double OverallHealthScore { get; set; }
        public Dictionary<int, double> PortHealthScores { get; set; } = new();
        public double ConnectivityReliability { get; set; }
        public UserActivitySummary UserActivitySummary { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
    }

    public class UserActivitySummary
    {
        public int TotalActions { get; set; }
        public int ConfigChanges { get; set; }
        public int SystemCommands { get; set; }
        public int FailedActions { get; set; }
        public int ActiveUsers { get; set; }
        public string? MostActiveUser { get; set; }
    }

    public class PortAnalytics
    {
        public int PortNumber { get; set; }
        public DateTime AnalysisDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public double UptimePercentage { get; set; }
        public TimeSpan TotalDowntime { get; set; }
        public TimeSpan? AverageDowntimeDuration { get; set; }
        public double ErrorRate { get; set; }
        public string ActivityLevel { get; set; } = "None";
        public DateTime? LastStatusChange { get; set; }
        public int StatusChanges { get; set; }
        public int ConfigChanges { get; set; }
    }

    public class Recommendation
    {
        public RecommendationType Type { get; set; }
        public RecommendationPriority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? AffectedEntity { get; set; }
        public double Confidence { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum RecommendationType
    {
        PortMaintenance,
        NetworkInfrastructure,
        UserTraining,
        ProcessImprovement,
        SecurityConcern,
        PerformanceOptimization
    }

    public enum RecommendationPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}