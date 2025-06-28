using System;
using System.Threading.Tasks;
using TPLinkWebUI.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TPLinkWebUI.Services
{
    public class SwitchService
    {
        private readonly CredentialsStorage _storage;
        private readonly ILogger<SwitchService> _logger;
        private TplinkClient? _client;

        public SwitchService(CredentialsStorage storage, ILogger<SwitchService> logger)
        {
            _storage = storage;
            _logger = logger;
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
            return SystemInfoResponse.Parse(systemInfoText);
        }

        public async Task<PortInfoResponse> GetPortInfoAsync()
        {
            await EnsureClientFromStorageAsync();
            var portInfoText = await _client!.GetPortInfoAsync();
            return PortInfoResponse.Parse(portInfoText);
        }

        public async Task<VlanConfigResponse> GetVlanConfigurationAsync()
        {
            await EnsureClientFromStorageAsync();
            var vlanConfigText = await _client!.GetVlanConfigurationAsync();
            return VlanConfigResponse.Parse(vlanConfigText);
        }

        public async Task SetPortConfigAsync(int port, bool enable, int speed = 1, bool flowControl = false)
        {
            await EnsureClientFromStorageAsync();
            await _client!.SetPortConfigAsync(port, enable, speed, flowControl);
        }

        public async Task<CableDiagnosticResponse> RunCableDiagnosticsAsync(int[] ports)
        {
            await EnsureClientFromStorageAsync();
            var result = await _client!.RunCableDiagnosticsAsync(ports);
            return CableDiagnosticResponse.FromTplinkResult(result);
        }

        public async Task CreateVlanAsync(int vlanId, int[] ports)
        {
            await EnsureClientFromStorageAsync();
            await _client!.CreatePortBasedVlanAsync(vlanId, ports);
        }

        public async Task DeleteVlansAsync(int[] vlanIds)
        {
            await EnsureClientFromStorageAsync();
            await _client!.DeletePortBasedVlanAsync(vlanIds);
        }

        public async Task RebootSwitchAsync()
        {
            await EnsureClientFromStorageAsync();
            await _client!.RebootAsync();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}