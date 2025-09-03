using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Resonance;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string AtProtocolHandle { get; set; } = string.Empty;
    public string AtProtocolPassword { get; set; } = string.Empty;
    public bool IsConfigured { get; set; } = false;
    public bool ShowSetupWindow { get; set; } = true;
    public bool ShowWelcomeMessage { get; set; } = true;
    
    public bool IsConfigWindowMovable { get; set; } = true;
    public bool EnableDebugLogging { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface?.SavePluginConfig(this);
    }
}