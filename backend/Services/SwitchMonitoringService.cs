using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TPLinkWebUI.Models;
using TPLinkWebUI.Services;

namespace TPLinkWebUI.Services;

public class SwitchMonitoringService : IHostedService, IDisposable
{
    private readonly ILogger<SwitchMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private System.Timers.Timer? _monitoringTimer;
    private System.Timers.Timer? _cookieRenewalTimer;
    private readonly SemaphoreSlim _monitoringSemaphore = new(1, 1);
    private readonly SemaphoreSlim _cookieRenewalSemaphore = new(1, 1);
    private bool _isAuthenticated = false;
    private bool _lastConnectionState = false;
    private DateTime _lastSuccessfulConnection = DateTime.MinValue;
    private DateTime _lastCookieRenewal = DateTime.MinValue;

    // Configuration
    private readonly TimeSpan _monitoringInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _cookieRenewalInterval = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _cookieLifetime = TimeSpan.FromMinutes(30);

    public SwitchMonitoringService(
        ILogger<SwitchMonitoringService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Switch Monitoring Service");

        // Initialize monitoring timer
        _monitoringTimer = new System.Timers.Timer(_monitoringInterval.TotalMilliseconds);
        _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
        _monitoringTimer.AutoReset = true;

        // Initialize cookie renewal timer
        _cookieRenewalTimer = new System.Timers.Timer(_cookieRenewalInterval.TotalMilliseconds);
        _cookieRenewalTimer.Elapsed += OnCookieRenewalTimerElapsed;
        _cookieRenewalTimer.AutoReset = true;

        // Try initial authentication
        await TryInitialAuthentication();

        // Start timers
        _monitoringTimer.Start();
        _cookieRenewalTimer.Start();

        _logger.LogInformation("Switch Monitoring Service started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Switch Monitoring Service");

        _monitoringTimer?.Stop();
        _cookieRenewalTimer?.Stop();

        await Task.CompletedTask;
    }

    private async Task TryInitialAuthentication()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var credentialsStorage = scope.ServiceProvider.GetRequiredService<CredentialsStorage>();
            var switchService = scope.ServiceProvider.GetRequiredService<SwitchService>();

            var credentials = await credentialsStorage.LoadAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No stored credentials found for initial authentication");
                return;
            }

            _logger.LogInformation("Attempting initial authentication to switch at {Host}", credentials.Host);

            await switchService.EnsureClientAsync(credentials);
            _isAuthenticated = true;
            _lastSuccessfulConnection = DateTime.UtcNow;
            _lastCookieRenewal = DateTime.UtcNow;
            _logger.LogInformation("Initial authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial authentication");
        }
    }

    private async void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!await _monitoringSemaphore.WaitAsync(TimeSpan.FromSeconds(1)))
        {
            _logger.LogDebug("Monitoring cycle skipped - already in progress");
            return;
        }

        try
        {
            await PerformMonitoringCycle();
        }
        finally
        {
            _monitoringSemaphore.Release();
        }
    }

    private async void OnCookieRenewalTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!await _cookieRenewalSemaphore.WaitAsync(TimeSpan.FromSeconds(1)))
        {
            _logger.LogDebug("Cookie renewal skipped - already in progress");
            return;
        }

        try
        {
            await PerformCookieRenewal();
        }
        finally
        {
            _cookieRenewalSemaphore.Release();
        }
    }

    private async Task PerformMonitoringCycle()
    {
        try
        {
            if (!_isAuthenticated)
            {
                _logger.LogDebug("Skipping monitoring cycle - not authenticated");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var switchService = scope.ServiceProvider.GetRequiredService<SwitchService>();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            var credentialsStorage = scope.ServiceProvider.GetRequiredService<CredentialsStorage>();

            _logger.LogDebug("Starting monitoring cycle");

            bool connectionSuccessful = false;
            string? switchIp = null;
            
            try
            {
                var credentials = await credentialsStorage.LoadAsync();
                switchIp = credentials?.Host;

                // Test connection and get system info
                var systemInfoResult = await switchService.GetSystemInfoAsync();
                _lastSuccessfulConnection = DateTime.UtcNow;
                connectionSuccessful = true;

                // Get port information
                var portsResult = await switchService.GetPortInfoAsync();
                if (portsResult.Ports != null)
                {
                    _logger.LogDebug("Monitoring cycle retrieved {PortCount} ports", portsResult.Ports.Count);
                    
                    // Port info is automatically logged by SwitchService with change detection
                    // No need to log again here to avoid duplicate entries
                }

                // Log system info to history
                await historyService.LogSystemInfoAsync(systemInfoResult, "monitoring");

                _logger.LogDebug("Monitoring cycle completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle - switch may be unreachable");
                connectionSuccessful = false;
                
                // Mark as unauthenticated on error
                _isAuthenticated = false;
            }

            // Log connectivity changes
            if (_lastConnectionState != connectionSuccessful)
            {
                if (connectionSuccessful)
                {
                    await historyService.LogSwitchConnectivityAsync(true, switchIp);
                    _logger.LogInformation("Switch became reachable");
                }
                else
                {
                    await historyService.LogSwitchConnectivityAsync(false, switchIp, errorMessage: "Switch unreachable during monitoring cycle");
                    _logger.LogWarning("Switch became unreachable");
                }
                _lastConnectionState = connectionSuccessful;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during monitoring cycle");
            _isAuthenticated = false;
            _lastConnectionState = false;
        }
    }

    private async Task PerformCookieRenewal()
    {
        try
        {
            if (!_isAuthenticated)
            {
                _logger.LogDebug("Skipping cookie renewal - not authenticated");
                return;
            }

            // Check if cookie renewal is needed
            var timeSinceLastRenewal = DateTime.UtcNow - _lastCookieRenewal;
            if (timeSinceLastRenewal < _cookieLifetime * 0.75) // Renew at 75% of lifetime
            {
                _logger.LogDebug("Cookie renewal not needed yet");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var credentialsStorage = scope.ServiceProvider.GetRequiredService<CredentialsStorage>();
            var switchService = scope.ServiceProvider.GetRequiredService<SwitchService>();

            var credentials = await credentialsStorage.LoadAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No stored credentials found for cookie renewal");
                _isAuthenticated = false;
                return;
            }

            _logger.LogInformation("Performing proactive cookie renewal");

            await switchService.EnsureClientAsync(credentials);
            _lastCookieRenewal = DateTime.UtcNow;
            _lastSuccessfulConnection = DateTime.UtcNow;
            _logger.LogInformation("Cookie renewal successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cookie renewal");
            _isAuthenticated = false;
        }
    }

    private async Task TryReconnect()
    {
        try
        {
            var timeSinceLastSuccess = DateTime.UtcNow - _lastSuccessfulConnection;
            if (timeSinceLastSuccess < _connectionTimeout)
            {
                _logger.LogDebug("Connection timeout not reached, skipping reconnect attempt");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var credentialsStorage = scope.ServiceProvider.GetRequiredService<CredentialsStorage>();
            var switchService = scope.ServiceProvider.GetRequiredService<SwitchService>();

            var credentials = await credentialsStorage.LoadAsync();
            if (credentials == null)
            {
                _logger.LogWarning("No stored credentials found for reconnection");
                return;
            }

            _logger.LogInformation("Attempting to reconnect to switch at {Host}", credentials.Host);

            await switchService.EnsureClientAsync(credentials);
            _isAuthenticated = true;
            _lastSuccessfulConnection = DateTime.UtcNow;
            _lastCookieRenewal = DateTime.UtcNow;
            _logger.LogInformation("Reconnection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reconnection attempt");
        }
    }

    public bool IsAuthenticated => _isAuthenticated;
    public DateTime LastSuccessfulConnection => _lastSuccessfulConnection;
    public DateTime LastCookieRenewal => _lastCookieRenewal;

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _cookieRenewalTimer?.Dispose();
        _monitoringSemaphore?.Dispose();
        _cookieRenewalSemaphore?.Dispose();
    }
}