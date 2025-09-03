# Resonance - Universal FFXIV Mod Sync Protocol

A decentralized, cross-client synchronization protocol for FFXIV modification and appearance data. Resonance enables seamless synchronization between different Mare forks including TeraSync, Neko Net, Lightless, Snowcloak, and any future clients.

## 🌟 Features

- **Universal Compatibility** - Works with any Mare-compatible client
- **Decentralized** - No central server dependency using AT Protocol
- **Zero Integration Effort** - Other clients just install and use IPC
- **Cross-Client Sync** - TeraSync users can sync with Neko Net users, etc.
- **Future-Proof** - Built on open standards (AT Protocol)

## 🚀 For Users

### Installation
1. Install Resonance from your Dalamud repository
2. Your existing mod sync client (TeraSync, Neko Net, etc.) will automatically detect and use Resonance
3. Enable cross-client sync in Resonance settings
4. Authenticate with your AT Protocol handle

## 🔧 For Developers

### Integration (2 lines of code!)

```csharp
// Publish your client's character data
PluginInterface.GetIpcProvider("Resonance.PublishData").SendMessage(characterData);

// Subscribe to incoming data from other clients
PluginInterface.GetIpcSubscriber<CharacterData>("Resonance.DataReceived").Subscribe(OnDataReceived);
```

That's it! Resonance handles all the AT Protocol complexity.

### Supported Clients
- ✅ TeraSync V2
- ✅ Neko Net Sync
- ✅ Lightless
- ✅ Snowcloak
- ✅ Any Mare-compatible fork

## 📡 How It Works

1. **Your client** publishes character/mod data via IPC to Resonance
2. **Resonance** converts it to AT Protocol records and publishes to the network
3. **Other Resonance instances** receive the data from the AT Protocol network
4. **Their clients** receive the data via IPC and apply it

```
TeraSync → Resonance → AT Protocol Network → Resonance → Neko Net
                            ↕                    ↕
                        Lightless            Snowcloak
```

## 🤝 Contributing

Resonance is a community project to reunite the FFXIV modding community. All Mare fork developers are welcome to contribute.

## 📜 License

GPL-3.0 - Same as Dalamud and other FFXIV plugins

## 🙏 Credits

- All Mare fork developers who made this necessary
- The AT Protocol team for the decentralized infrastructure
- The FFXIV modding community