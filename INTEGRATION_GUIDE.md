# Resonance Integration Guide for Mare Fork Developers

## Quick Start (2 minutes)

### Step 1: Install Resonance
Users install Resonance alongside your Mare fork from the Dalamud repository.

### Step 2: Add IPC calls to your client

```csharp
// In your plugin initialization
IDalamudPluginInterface pluginInterface;

// Subscribe to incoming data from other clients
var dataReceiver = pluginInterface.GetIpcSubscriber<Dictionary<string, object>, string, object>("Resonance.DataReceived");
dataReceiver.Subscribe(OnResonanceDataReceived);

// When you want to publish your character data
var publisher = pluginInterface.GetIpcSubscriber<Dictionary<string, object>, bool>("Resonance.PublishData");
publisher.InvokeFunc(ConvertToResonanceFormat(yourCharacterData));
```

### Step 3: Convert your data format

```csharp
private Dictionary<string, object> ConvertToResonanceFormat(YourCharacterData data)
{
    return new Dictionary<string, object>
    {
        ["CharacterName"] = data.Name,
        ["WorldName"] = data.World,
        ["GlamourerData"] = data.GlamourerString,
        ["FileReplacements"] = data.ModFiles,
        ["MoodlesData"] = data.Moodles,
        // Add other fields as needed
        ["SourceClient"] = "YourClientName" // e.g., "TeraSync", "NekoNet"
    };
}

private void OnResonanceDataReceived(Dictionary<string, object> data, string senderDid)
{
    // Apply the received data to the character
    if (data.TryGetValue("GlamourerData", out var glamData))
    {
        ApplyGlamourerData(glamData as string);
    }
    
    if (data.TryGetValue("FileReplacements", out var files))
    {
        ApplyModFiles(files as Dictionary<string, string>);
    }
    
    // Handle other data fields...
}
```

## Complete IPC Interface

### Publishing Data

**IPC Name:** `Resonance.PublishData`  
**Parameters:** `Dictionary<string, object>` - Character data  
**Returns:** `bool` - Success status  

### Authentication

**IPC Name:** `Resonance.Authenticate`  
**Parameters:** `string` - AT Protocol handle (e.g., "alice.bsky.social")  
**Returns:** `bool` - Success status  

### Check Authentication Status

**IPC Name:** `Resonance.IsAuthenticated`  
**Parameters:** None  
**Returns:** `bool` - Authentication status  

### Get Connected Clients

**IPC Name:** `Resonance.GetConnectedClients`  
**Parameters:** None  
**Returns:** `List<string>` - List of connected client DIDs  

## Events You Can Subscribe To

### Data Received

**IPC Name:** `Resonance.DataReceived`  
**Parameters:** 
- `Dictionary<string, object>` - Character data
- `string` - Sender's DID

### Client Connected

**IPC Name:** `Resonance.ClientConnected`  
**Parameters:** `string` - Connected client's DID

### Client Disconnected

**IPC Name:** `Resonance.ClientDisconnected`  
**Parameters:** `string` - Disconnected client's DID

## Data Format Specification

All data is passed as `Dictionary<string, object>` for maximum flexibility. Standard fields:

```csharp
{
    // Required fields
    "CharacterName": "Character Name",
    "WorldName": "Server Name",
    "SourceClient": "TeraSync", // Your client name
    "Version": 1,
    "CreatedAt": "2025-01-01T00:00:00Z",
    
    // Optional fields (include what you support)
    "GlamourerData": "base64_encoded_glamourer_data",
    "FileReplacements": { "game/path.mdl": "mod_file_hash" },
    "ManipulationData": "penumbra_manipulation_string",
    "MoodlesData": "base64_encoded_moodles",
    "HeelsData": "heels_offset_data",
    "CustomizePlusData": "cplus_scale_data",
    "HonorificData": "honorific_title_data",
    
    // Client-specific data (for your unique features)
    "ClientSpecificData": {
        "YourSpecialFeature": "value"
    }
}
```

## Example: TeraSync Integration

```csharp
public class TeraSyncResonanceIntegration
{
    private ICallGateSubscriber<Dictionary<string, object>, bool> _publisher;
    private ICallGateSubscriber<Dictionary<string, object>, string, object> _receiver;
    
    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        // Set up publishing
        _publisher = pluginInterface.GetIpcSubscriber<Dictionary<string, object>, bool>("Resonance.PublishData");
        
        // Set up receiving
        _receiver = pluginInterface.GetIpcSubscriber<Dictionary<string, object>, string, object>("Resonance.DataReceived");
        _receiver.Subscribe(OnDataReceived);
    }
    
    public void PublishCharacterData(CharacterData data)
    {
        var resonanceData = new Dictionary<string, object>
        {
            ["CharacterName"] = data.Name,
            ["WorldName"] = data.World,
            ["GlamourerData"] = data.GlamourerString,
            ["FileReplacements"] = data.FileReplacements.ToDictionary(
                fr => fr.GamePaths.First(),
                fr => fr.Hash
            ),
            ["MoodlesData"] = data.MoodlesData,
            ["SourceClient"] = "TeraSync",
            ["Version"] = 1,
            ["CreatedAt"] = DateTime.UtcNow
        };
        
        _publisher.InvokeFunc(resonanceData);
    }
    
    private void OnDataReceived(Dictionary<string, object> data, string senderDid)
    {
        // Only process data from other clients, not our own
        if (data["SourceClient"] as string == "TeraSync") return;
        
        // Apply the received data
        ApplyCharacterData(data);
    }
}
```

## Testing Your Integration

1. Install Resonance on two different game instances
2. Install your Mare fork on one instance
3. Install a different Mare fork on the other instance
4. Both should be able to see each other's character data!

## Support

- Discord: [Your Discord Server]
- GitHub Issues: https://github.com/EchoEngineering/ResonanceFFXIV/issues

## FAQ

**Q: Do I need to modify my networking code?**  
A: No! Resonance handles all AT Protocol networking. You just send/receive data via IPC.

**Q: What if Resonance isn't installed?**  
A: Check if the IPC gates exist before using them. Your client should work normally without Resonance.

**Q: Can I still use my own sync protocol?**  
A: Yes! Resonance is additive. You can support both your protocol and Resonance simultaneously.

**Q: What about file transfers?**  
A: Resonance handles file hashes. Actual file transfer can be done via your existing CDN or Resonance's blob storage (coming soon).