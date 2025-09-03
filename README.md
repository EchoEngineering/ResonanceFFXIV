# Resonance - Universal FFXIV Mod Sync Protocol

**Sync your FFXIV mods and appearance across different clients!** Resonance enables TeraSync users to sync with Neko Net users (and vice versa) without everyone needing to use the same client.

---

## 🎮 For Users

### What is Resonance?
If you use **TeraSync** and your friend uses **Neko Net**, you normally can't sync with each other. Resonance fixes this by creating a universal sync layer that works with any Mare-compatible client.

### Quick Start
1. **Install Resonance** from your Dalamud plugin installer (search "Resonance")
2. **Install on ALL devices** where you want cross-client sync to work
3. **Authenticate** with your handle (like alice.bsky.social) in Resonance settings
4. **That's it!** Your existing mod sync client will now work with other clients too

### What You Need
- ✅ **Any Mare-compatible client** (TeraSync, Neko Net, etc.)
- ✅ **Dalamud** (like all FFXIV plugins)
- ✅ **AT Protocol handle** (free - get one at bsky.app)

### Current Status
- ✅ **Ready for testing** between TeraSync and Neko Net
- 🚧 **More clients coming soon** as they add support

---

## 💻 For Developers (Mare Fork Authors)

### Why Add Resonance Support?

**Problem:** Your users can only sync with people using your exact client  
**Solution:** 2 lines of code = your users can sync with ALL Mare clients

### Integration Steps

#### 1. Check if Resonance is Available
```csharp
private bool HasResonance()
{
    try 
    {
        PluginInterface.GetIpcSubscriber<bool>("Resonance.IsAvailable");
        return true;
    }
    catch { return false; }
}
```

#### 2. Publish Your Data (when character changes)
```csharp
if (HasResonance())
{
    var data = ConvertToResonanceFormat(yourCharacterData);
    PluginInterface.GetIpcProvider<Dictionary<string, object>, bool>("Resonance.PublishData")
        .SendMessage(data);
}
```

#### 3. Receive Other Clients' Data
```csharp
if (HasResonance())
{
    PluginInterface.GetIpcSubscriber<Dictionary<string, object>, string, object>("Resonance.DataReceived")
        .Subscribe(OnResonanceDataReceived);
}

private void OnResonanceDataReceived(Dictionary<string, object> data, string senderDid)
{
    // Apply the character data just like you do with your own protocol
    ApplyCharacterData(ConvertFromResonanceFormat(data));
}
```

### Data Format
```csharp
var resonanceData = new Dictionary<string, object>
{
    ["CharacterName"] = "Character Name",
    ["WorldName"] = "Server Name", 
    ["GlamourerData"] = glamourerString,           // Base64 Glamourer data
    ["FileReplacements"] = modFilesDictionary,     // Game path -> file hash
    ["MoodlesData"] = moodlesString,               // Moodles status
    ["SourceClient"] = "YourClientName",           // So you can filter your own
    ["Version"] = 1
};
```

### Complete Integration Example
See [INTEGRATION_GUIDE.md](./INTEGRATION_GUIDE.md) for full code examples.

---

## ❓ FAQ

### For Users

**Q: Do I still need my regular mod sync client?**  
A: Yes! Resonance is an add-on that makes different clients work together.

**Q: What happens if my friend doesn't have Resonance?**  
A: Everything works normally. Resonance only adds cross-client sync, it doesn't break anything.

**Q: Is my data safe?**  
A: Yes. Resonance uses AT Protocol, the same decentralized tech that powers Bluesky.

**Q: Will this slow down my game?**  
A: No. Resonance only activates when you're syncing appearance data.

### For Developers

**Q: Will this break my existing sync?**  
A: No. Resonance is completely optional and runs alongside your current protocol.

**Q: What if Resonance isn't installed?**  
A: Check for availability first. Your client works normally without it.

**Q: Do I need to change my networking code?**  
A: No! You just publish/receive via IPC. Resonance handles all the AT Protocol networking.

---

## 🔧 Technical Details

### Architecture
```
Your Client → IPC → Resonance → AT Protocol Network → Resonance → Other Client
```

### Dependencies
- Dalamud.NET.Sdk 13.0.0
- System.Text.Json
- AT Protocol client libraries

### AT Protocol Schemas
- `xyz.ffxiv.resonance.character` - Character appearance data
- `xyz.ffxiv.resonance.permission` - Cross-client permissions

---

## 🤝 Contributing

Want to help reunite the FFXIV modding community?

- **Users:** Test cross-client sync and report issues
- **Developers:** Add Resonance support to your Mare fork
- **Community:** Spread the word about universal sync

### Development Setup
```bash
git clone https://github.com/EchoEngineering/ResonanceFFXIV.git
cd ResonanceFFXIV/Resonance
dotnet build
```

## 📜 License

AGPL-3.0 - Following Dalamud's licensing

## 🙏 Credits

- Mare fork developers for creating the ecosystem
- AT Protocol team for decentralized infrastructure  
- FFXIV modding community for feedback and testing