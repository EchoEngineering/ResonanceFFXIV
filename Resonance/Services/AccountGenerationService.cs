using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Resonance.Services;

public class AccountGenerationService
{
    private readonly IPluginLog _logger;
    private readonly AtProtocolClient _atProtocolClient;
    private readonly HttpClient _httpClient;
    
    public AccountGenerationService(IPluginLog logger, AtProtocolClient atProtocolClient)
    {
        _logger = logger;
        _atProtocolClient = atProtocolClient;
        _httpClient = new HttpClient();
    }
    
    /// <summary>
    /// Generates a unique handle for FFXIV sync
    /// Format: ffxiv-sync-{8-char-hash}.sync.terasync.app
    /// </summary>
    public string GenerateHandle()
    {
        // Create a unique identifier using timestamp + random data
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomBytes = new byte[4];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        // Combine timestamp and random data
        var combined = BitConverter.GetBytes(timestamp).AsSpan().ToArray()
            .Concat(randomBytes).ToArray();
            
        // Create a short hash
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(combined);
            var shortHash = Convert.ToHexString(hash)[..8].ToLowerInvariant();
            return $"ffxiv-sync-{shortHash}.sync.terasync.app";
        }
    }
    
    /// <summary>
    /// Generates a secure random password for the auto-account
    /// </summary>
    public string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var password = new StringBuilder();
        
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[24]; // 24 character password
            rng.GetBytes(bytes);
            
            foreach (var b in bytes)
            {
                password.Append(chars[b % chars.Length]);
            }
        }
        
        return password.ToString();
    }

    /// <summary>
    /// Generates a temporary email address for account creation
    /// Uses the same hash as the handle for consistency
    /// </summary>
    public string GenerateEmail(string handle)
    {
        // Extract the hash from the handle (ffxiv-sync-{hash}.sync.terasync.app)
        var parts = handle.Split('-');
        if (parts.Length >= 3)
        {
            var hashPart = parts[2].Split('.')[0]; // Get hash before .sync.terasync.app
            return $"ffxiv-sync-{hashPart}@tempmail.plus";
        }
        
        // Fallback if handle format is unexpected
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(handle));
            var shortHash = Convert.ToHexString(hash)[..8].ToLowerInvariant();
            return $"ffxiv-sync-{shortHash}@tempmail.plus";
        }
    }
    
    /// <summary>
    /// Checks if a handle is available on the appropriate PDS
    /// </summary>
    public async Task<(bool Available, string ErrorMessage)> CheckHandleAvailabilityAsync(string handle)
    {
        try
        {
            // Use appropriate PDS based on handle domain
            var pdsEndpoint = handle.EndsWith(".sync.terasync.app") ? "https://sync.terasync.app" : "https://bsky.social";
            var resolveUrl = $"{pdsEndpoint}/xrpc/com.atproto.identity.resolveHandle?handle={handle}";
            
            var response = await _httpClient.GetAsync(resolveUrl);
            
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (content.Contains("Unable to resolve handle"))
                {
                    return (true, string.Empty);
                }
            }
            
            if (response.IsSuccessStatusCode)
            {
                return (false, "Handle is already taken");
            }
            
            return (false, $"Error checking handle availability: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to check handle availability for {handle}");
            return (false, $"Failed to check handle availability: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an account on the appropriate PDS based on handle domain
    /// </summary>
    private async Task<(bool Success, string ErrorMessage)> CreateBlueskyAccountAsync(string handle, string password)
    {
        try
        {
            // Use our self-hosted PDS for .sync.terasync.app handles
            var pdsEndpoint = handle.EndsWith(".sync.terasync.app") ? "https://sync.terasync.app" : "https://bsky.social";
            var createAccountUrl = $"{pdsEndpoint}/xrpc/com.atproto.server.createAccount";
            
            var email = GenerateEmail(handle);
            
            var requestData = new
            {
                handle = handle,
                email = email,
                password = password
            };
            
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.Info($"Attempting to create account for handle: {handle} with email: {email}");
            var response = await _httpClient.PostAsync(createAccountUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"Successfully created account: {handle}");
                return (true, string.Empty);
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.Warning($"Account creation failed: {response.StatusCode} - {errorContent}");
            
            if (errorContent.Contains("HandleNotAvailable"))
            {
                return (false, "Handle is already taken");
            }
            if (errorContent.Contains("InvalidHandle"))
            {
                return (false, "Handle format is invalid");
            }
            if (errorContent.Contains("InvalidPassword"))
            {
                return (false, "Password does not meet requirements");
            }
            if (errorContent.Contains("InvalidInviteCode"))
            {
                return (false, "Invite code required but not provided");
            }
            if (errorContent.Contains("UnsupportedDomain"))
            {
                return (false, "Handle domain not supported");
            }
            if (errorContent.Contains("InvalidRequest") && errorContent.Contains("Email"))
            {
                return (false, "Email validation failed");
            }
            if (errorContent.Contains("InvalidPhoneVerification") || errorContent.Contains("Verification is now required"))
            {
                return (false, "Phone verification is now required by Bluesky. Please use the Advanced Setup to connect with your existing Bluesky account instead.");
            }
            
            return (false, $"Account creation failed: {errorContent}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Exception during account creation for {handle}");
            return (false, $"Account creation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an anonymous Bluesky account automatically with retry logic for taken handles
    /// </summary>
    public async Task<(bool Success, string Handle, string Password, string ErrorMessage)> CreateAutoAccountAsync()
    {
        try
        {
            _logger.Info("Starting auto-account creation...");
            
            var password = GeneratePassword();
            var maxRetries = 5;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var handle = GenerateHandle();
                _logger.Info($"Attempt {attempt + 1}: Checking handle availability for: {handle}");
                
                var (available, checkError) = await CheckHandleAvailabilityAsync(handle);
                if (!available)
                {
                    if (!string.IsNullOrEmpty(checkError) && !checkError.Contains("taken"))
                    {
                        _logger.Warning($"Handle check failed with error: {checkError}");
                        return (false, string.Empty, string.Empty, checkError);
                    }
                    
                    _logger.Info($"Handle {handle} is taken, trying another...");
                    continue;
                }
                
                _logger.Info($"Handle {handle} is available, attempting to create account...");
                var (success, createError) = await CreateBlueskyAccountAsync(handle, password);
                
                if (success)
                {
                    _logger.Info($"Successfully created auto-account: {handle}");
                    return (true, handle, password, string.Empty);
                }
                
                if (createError.Contains("taken"))
                {
                    _logger.Info($"Handle {handle} was taken during creation, retrying...");
                    continue;
                }
                
                _logger.Warning($"Account creation failed: {createError}");
                return (false, handle, password, createError);
            }
            
            _logger.Warning($"Failed to create account after {maxRetries} attempts - all handles were taken");
            return (false, string.Empty, string.Empty, "Unable to find available handle after multiple attempts. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create auto-account");
            return (false, string.Empty, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Creates an account with a user-specified handle (for custom handles)
    /// </summary>
    public async Task<(bool Success, string Handle, string Password, string ErrorMessage)> CreateCustomAccountAsync(string userHandle)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userHandle))
            {
                return (false, string.Empty, string.Empty, "Handle cannot be empty");
            }
            
            var handle = $"{userHandle.ToLowerInvariant().Replace(" ", "-")}.sync.terasync.app";
            var password = GeneratePassword();
            
            _logger.Info($"Checking availability for custom handle: {handle}");
            var (available, checkError) = await CheckHandleAvailabilityAsync(handle);
            
            if (!available)
            {
                return (false, handle, password, string.IsNullOrEmpty(checkError) ? "Handle is already taken" : checkError);
            }
            
            _logger.Info($"Creating custom account with handle: {handle}");
            var (success, createError) = await CreateBlueskyAccountAsync(handle, password);
            
            if (success)
            {
                _logger.Info($"Successfully created custom account: {handle}");
                return (true, handle, password, string.Empty);
            }
            
            return (false, handle, password, createError);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to create custom account for handle: {userHandle}");
            return (false, string.Empty, string.Empty, ex.Message);
        }
    }
    
    /// <summary>
    /// Validates that a Resonance handle is valid (alphanumeric + basic chars)
    /// AT Protocol handles have a 253 character limit total, and we use .sync.terasync.app (18 chars)
    /// So the user portion can be max 235 chars, but we'll be more conservative
    /// </summary>
    public bool IsValidResonanceHandle(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return false;
            
        // Conservative limit: user part + .sync.terasync.app should stay well under 253 chars
        // .sync.terasync.app = 18 chars, so allow max 30 for user part to be safe
        if (handle.Length > 30)
            return false;
            
        // Allow alphanumeric, spaces, dashes, underscores
        foreach (char c in handle)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
                return false;
        }
        
        return true;
    }
    
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}