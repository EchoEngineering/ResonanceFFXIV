using System;
using System.Collections.Generic;

namespace Resonance.Models;

/// <summary>
/// Universal character data format that all Mare forks can understand
/// This is the common interchange format for cross-client synchronization
/// </summary>
public class UniversalCharacterData
{
    /// <summary>
    /// Character name
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;
    
    /// <summary>
    /// World/Server name
    /// </summary>
    public string WorldName { get; set; } = string.Empty;
    
    /// <summary>
    /// Glamourer customization data (Base64 encoded)
    /// Compatible with all clients that use Glamourer
    /// </summary>
    public string? GlamourerData { get; set; }
    
    /// <summary>
    /// Penumbra mod list with file replacements
    /// Key: Game file path, Value: Mod file hash
    /// </summary>
    public Dictionary<string, string>? FileReplacements { get; set; }
    
    /// <summary>
    /// Penumbra manipulation data (metadata changes)
    /// </summary>
    public string? ManipulationData { get; set; }
    
    /// <summary>
    /// Moodles status effects (Base64 encoded)
    /// </summary>
    public string? MoodlesData { get; set; }
    
    /// <summary>
    /// SimpleHeels offset data
    /// </summary>
    public string? HeelsData { get; set; }
    
    /// <summary>
    /// Customize+ scale data
    /// </summary>
    public string? CustomizePlusData { get; set; }
    
    /// <summary>
    /// Honorific title/name data
    /// </summary>
    public string? HonorificData { get; set; }
    
    /// <summary>
    /// Client-specific data that other clients might not understand
    /// Each client can put their unique data here
    /// </summary>
    public Dictionary<string, object>? ClientSpecificData { get; set; }
    
    /// <summary>
    /// Timestamp when this data was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Version of the data format (for future compatibility)
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// Source client identifier (e.g., "TeraSync", "NekoNet", etc.)
    /// </summary>
    public string SourceClient { get; set; } = "Unknown";
}

/// <summary>
/// Simplified file replacement format for cross-client compatibility
/// </summary>
public class FileReplacement
{
    public string GamePath { get; set; } = string.Empty;
    public string ModPath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}