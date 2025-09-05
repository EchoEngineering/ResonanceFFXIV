# Resonance: Universal Cross-Client Mare Sync Integration

## What is Resonance?

Resonance is a universal synchronization protocol that enables different Mare forks to sync with each other. Instead of each Mare client only syncing within its own ecosystem, Resonance creates bridges between TeraSync, Neko Net, and any other Mare-based clients.

**The Problem We Solved:**
- Mare forks were isolated ecosystems - TeraSync users could only sync with other TeraSync users
- Users had to convince their friends to use the same client
- Community fragmentation limited the social aspect of Mare synchronization

**Our Solution:**
- Universal protocol that works across different Mare implementations
- Users can sync regardless of which Mare client they prefer
- Preserves each client's unique features while enabling cross-compatibility

## Why AT Protocol / Bluesky?

We chose AT Protocol (the decentralized protocol behind Bluesky) for several strategic reasons:

### Technical Advantages
- **Decentralized**: No single point of failure, no central server to maintain
- **Mature Protocol**: Battle-tested infrastructure with millions of users
- **Built for Social**: Designed specifically for social interactions and data sharing
- **Cryptographic Security**: End-to-end encryption and identity verification built-in
- **Scalable**: Handles massive user bases without performance degradation

### User Experience Benefits
- **Easy Onboarding**: Users can use existing Bluesky accounts OR get anonymous accounts automatically
- **No Phone Verification**: Our self-hosted PDS (`sync.terasync.app`) bypasses Bluesky's phone requirements
- **Privacy Focused**: Anonymous accounts available, data stays encrypted in transit
- **Familiar Interface**: Users already understand social media concepts like handles and DIDs

### Infrastructure Benefits
- **No Server Costs**: We don't need to maintain expensive synchronization servers
- **Self-Sustaining**: Protocol continues working even if we stop developing
- **Future-Proof**: Built on web standards, will work for years to come
- **Reliability**: Leverages Bluesky's enterprise-grade infrastructure

## How Integration Works

### For Your Users
1. Install your Mare client (unchanged functionality)
2. Install Resonance plugin alongside it
3. Set up Resonance account (automatic anonymous or manual Bluesky)
4. Share Sync ID with friends using any Mare client
5. Enjoy cross-client synchronization

### For Your Development Team
You implement a few simple IPC providers and Resonance handles everything else:

```csharp
// 1. Let Resonance detect your client is running
pluginInterface.GetIpcProvider<bool>("YourClient.IsAvailable").RegisterFunc(() => true);

// 2. Export character data when requested
pluginInterface.GetIpcProvider<string>("YourClient.ExportCharacterData").RegisterFunc(() => {
    return JsonSerializer.Serialize(new {
        GlamourerData = GetGlamourerData(),
        PenumbraMods = GetActiveMods(),
        // ... other mod data
    });
});

// 3. Import character data from other clients
pluginInterface.GetIpcProvider<string, bool>("YourClient.ImportCharacterData").RegisterAction((jsonData) => {
    var data = JsonSerializer.Deserialize<CharacterData>(jsonData);
    ApplyToYourClient(data);
    return true;
});
```

That's literally it. Resonance handles:
- AT Protocol communication
- Data encryption/decryption
- Network reliability and retry logic
- User authentication and identity
- Cross-client data format translation

## What This Means for Your Community

### Immediate Benefits
- **Larger Sync Community**: Your users can sync with TeraSync, Neko Net, and future Mare clients
- **User Choice**: People can use their preferred client without losing friends
- **Network Effects**: More users in the sync ecosystem benefits everyone

### Long-term Strategic Value
- **Future-Proofing**: Your client becomes part of a growing universal ecosystem
- **Reduced Development**: No need to build your own networking infrastructure
- **Community Growth**: Easier for new users to join when they can sync with existing friends

### Competitive Advantages
- **Differentiation**: Be one of the first Mare clients with cross-client compatibility
- **User Retention**: Users won't leave for other clients just to sync with friends
- **Innovation Focus**: Spend development time on unique features instead of networking

## Implementation Process

### Phase 1: Basic Integration (2-4 hours)
1. Add the 3 IPC providers above
2. Test detection with existing Resonance users
3. Verify data export/import works

### Phase 2: Testing (1-2 weeks)
1. Internal testing with your team
2. Beta testing with a few community members
3. Cross-client testing with TeraSync users

### Phase 3: Launch
1. Announce cross-client sync capability
2. Update your documentation
3. Coordinate with us for any issues

## Technical Support

We provide full technical support during integration:

- **Code Review**: We'll review your IPC implementation
- **Testing Environment**: Access to our test infrastructure
- **Direct Support**: GitHub issues or direct communication
- **Documentation**: Complete API documentation and examples
- **Reference Implementation**: Full TeraSync integration available as example

## Data Format Compatibility

Resonance automatically handles differences between Mare implementations:

### What We Support
- **Glamourer**: Appearance customizations and equipment
- **Penumbra**: Mod lists and configurations  
- **Moodles**: Status effects and emotes
- **SimpleHeels**: Height adjustments
- **Customize+**: Body modifications
- **Future Mods**: Protocol designed to be extensible

### How It Works
- Your client exports data in whatever format you use
- Resonance translates between different implementations
- Remote client receives data in their expected format
- Users see consistent appearance regardless of client differences

## Getting Started

**Interested in integrating?** Here's how to begin:

1. **Review this document** and ask any questions
2. **Clone Resonance repository**: https://github.com/EchoEngineering/ResonanceFFXIV
3. **Examine TeraSync integration** in the codebase as reference
4. **Implement basic IPC providers** following our examples
5. **Test with our team** to verify everything works
6. **Launch to your community** with cross-client sync

## Why Join Now?

- **First Mover Advantage**: Be among the first Mare clients with universal sync
- **Growing Network**: More clients joining means more value for everyone
- **Proven Infrastructure**: Already working with TeraSync, battle-tested
- **Community Demand**: Users are asking for cross-client compatibility
- **Zero Risk**: Integration doesn't change your existing functionality

## Questions?

- **GitHub Issues**: https://github.com/EchoEngineering/ResonanceFFXIV/issues
- **Technical Questions**: We're available for direct consultation
- **Integration Support**: Full support throughout the process

Ready to give your users universal Mare sync? Let's make it happen! 🚀