using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Services;

public class AccountGenerationService
{
    private readonly IPluginLog _logger;
    private readonly AtProtocolClient _atProtocolClient;
    
    public AccountGenerationService(IPluginLog logger, AtProtocolClient atProtocolClient)
    {
        _logger = logger;
        _atProtocolClient = atProtocolClient;
    }
    
    /// <summary>
    /// Generates a unique handle for FFXIV sync
    /// Format: ffxiv-sync-{8-char-hash}.bsky.social
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
            return $"ffxiv-sync-{shortHash}.bsky.social";
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
    /// Creates an anonymous Bluesky account automatically
    /// </summary>
    public Task<(bool Success, string Handle, string Password, string ErrorMessage)> CreateAutoAccountAsync()
    {
        try
        {
            _logger.Info("Starting auto-account creation...");
            
            var handle = GenerateHandle();
            var password = GeneratePassword();
            
            _logger.Info($"Generated handle: {handle}");
            
            // TODO: Implement actual Bluesky account creation
            // This would need to call Bluesky's account creation API
            // For now, we'll simulate this and require manual account creation
            
            _logger.Warning("Auto-account creation not yet implemented - falling back to manual setup");
            return Task.FromResult((false, handle, password, "Auto-account creation not yet implemented. Please create a Bluesky account manually."));
            
            // Future implementation would look like:
            // var success = await CreateBlueskyAccountAsync(handle, password);
            // if (success)
            // {
            //     _logger.Info($"Successfully created auto-account: {handle}");
            //     return (true, handle, password, string.Empty);
            // }
            // else
            // {
            //     return (false, handle, password, "Failed to create Bluesky account");
            // }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create auto-account");
            return Task.FromResult((false, string.Empty, string.Empty, ex.Message));
        }
    }
    
    /// <summary>
    /// Validates that a Resonance handle is valid (alphanumeric + basic chars)
    /// </summary>
    public bool IsValidResonanceHandle(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return false;
            
        if (handle.Length > 32) // Reasonable limit
            return false;
            
        // Allow alphanumeric, spaces, dashes, underscores
        foreach (char c in handle)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
                return false;
        }
        
        return true;
    }
}