using System.Text.Json;
using System.Text;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Resonance.Services;

public class AtProtocolClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IPluginLog _logger;
    
    private string? _accessJwt;
    private string? _refreshJwt;
    private string? _did;
    private string? _handle;
    private string? _pdsEndpoint;
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessJwt) && !string.IsNullOrEmpty(_did);
    public string? CurrentDid => _did;
    public string? CurrentHandle => _handle;
    
    public AtProtocolClient(IPluginLog logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Resonance/1.0.0 FFXIV");
    }
    
    public async Task<bool> AuthenticateAsync(string handle, string password)
    {
        try
        {
            _logger.Info($"Attempting to authenticate with handle: {handle}");
            
            // Step 1: Resolve PDS endpoint from handle
            var pdsEndpoint = await ResolvePdsEndpointAsync(handle);
            if (pdsEndpoint == null)
            {
                _logger.Error("Failed to resolve PDS endpoint");
                return false;
            }
            
            _pdsEndpoint = pdsEndpoint;
            _logger.Info($"Resolved PDS endpoint: {pdsEndpoint}");
            
            // Step 2: Create session
            var sessionRequest = new
            {
                identifier = handle,
                password = password
            };
            
            var requestJson = JsonSerializer.Serialize(sessionRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            var sessionUrl = $"{pdsEndpoint}/xrpc/com.atproto.server.createSession";
            var response = await _httpClient.PostAsync(sessionUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.Error($"Authentication failed: {response.StatusCode} - {errorContent}");
                return false;
            }
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var sessionResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            _accessJwt = sessionResponse.GetProperty("accessJwt").GetString();
            _refreshJwt = sessionResponse.GetProperty("refreshJwt").GetString();
            _did = sessionResponse.GetProperty("did").GetString();
            _handle = sessionResponse.GetProperty("handle").GetString();
            
            _logger.Info($"Successfully authenticated as: {_handle} ({_did})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Authentication failed");
            return false;
        }
    }
    
    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshJwt) || string.IsNullOrEmpty(_pdsEndpoint))
        {
            _logger.Warning("No refresh token or PDS endpoint available");
            return false;
        }
        
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _refreshJwt);
            
            var refreshUrl = $"{_pdsEndpoint}/xrpc/com.atproto.server.refreshSession";
            var response = await _httpClient.PostAsync(refreshUrl, null);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.Error($"Token refresh failed: {response.StatusCode} - {errorContent}");
                return false;
            }
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var refreshResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            _accessJwt = refreshResponse.GetProperty("accessJwt").GetString();
            _refreshJwt = refreshResponse.GetProperty("refreshJwt").GetString();
            
            _logger.Debug("Successfully refreshed access token");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Token refresh failed");
            return false;
        }
    }
    
    public async Task<bool> PublishCharacterDataAsync(Dictionary<string, object> characterData)
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(_pdsEndpoint))
        {
            _logger.Warning("Not authenticated or no PDS endpoint");
            return false;
        }
        
        try
        {
            // Generate a timestamp-based record key (TID)
            var recordKey = GenerateRecordKey();
            
            var recordData = new
            {
                collection = "xyz.ffxiv.resonance.character",
                repo = _did,
                rkey = recordKey,
                record = characterData
            };
            
            var requestJson = JsonSerializer.Serialize(recordData);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessJwt);
            
            var putRecordUrl = $"{_pdsEndpoint}/xrpc/com.atproto.repo.putRecord";
            var response = await _httpClient.PostAsync(putRecordUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.Error($"Failed to publish character data: {response.StatusCode} - {errorContent}");
                
                // Try token refresh on 401
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    if (await RefreshTokenAsync())
                    {
                        return await PublishCharacterDataAsync(characterData);
                    }
                }
                
                return false;
            }
            
            _logger.Info($"Successfully published character data with record key: {recordKey}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to publish character data");
            return false;
        }
    }
    
    private Task<string?> ResolvePdsEndpointAsync(string handle)
    {
        try
        {
            // Route to appropriate PDS based on handle domain
            if (handle.EndsWith(".sync.terasync.app"))
            {
                _logger.Info($"Using self-hosted PDS for handle: {handle}");
                return Task.FromResult<string?>("http://sync.terasync.app");
            }
            
            if (handle.EndsWith(".bsky.social"))
            {
                _logger.Info($"Using Bluesky PDS for handle: {handle}");
                return Task.FromResult<string?>("https://bsky.social");
            }
            
            // Default to main Bluesky instance for other handles
            _logger.Info($"Using default Bluesky PDS for handle: {handle}");
            return Task.FromResult<string?>("https://bsky.social");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to resolve PDS endpoint for {handle}");
            return Task.FromResult<string?>(null);
        }
    }
    
    private string GenerateRecordKey()
    {
        // Generate a timestamp-based TID (Timestamp Identifier)
        // This provides chronological sorting as recommended by AT Protocol
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new Random().Next(1000, 9999);
        return $"{timestamp}-{random}";
    }
    
    public void Logout()
    {
        _accessJwt = null;
        _refreshJwt = null;
        _did = null;
        _handle = null;
        _pdsEndpoint = null;
        
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _logger.Info("Logged out successfully");
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}