using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Resonance.Services;
using Resonance.Windows;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Resonance;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Resonance";
    
    private readonly IDalamudPluginInterface _pluginInterface;
    public IDalamudPluginInterface PluginInterface => _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IPluginLog _log;
    
    // Window Management
    public readonly WindowSystem WindowSystem = new("Resonance");
    private readonly MainWindow _mainWindow;
    private readonly ConfigWindow _configWindow;
    
    // Configuration
    private readonly Configuration _configuration;
    
    // AT Protocol Client
    private readonly AtProtocolClient _atProtocolClient;
    
    // Account Generation Service
    private readonly AccountGenerationService _accountGenerationService;
    
    // IPC Providers - What Resonance offers to other plugins
    private readonly ICallGateProvider<Dictionary<string, object>, bool> _publishDataGate;
    private readonly ICallGateProvider<string, bool> _authenticateGate;
    private readonly ICallGateProvider<bool> _isAuthenticatedGate;
    private readonly ICallGateProvider<List<string>> _getConnectedClientsGate;
    
    // IPC Subscribers - Events Resonance sends to other plugins
    private readonly ICallGateProvider<Dictionary<string, object>, string, object> _dataReceivedGate;
    private readonly ICallGateProvider<string, object> _clientConnectedGate;
    private readonly ICallGateProvider<string, object> _clientDisconnectedGate;
    
    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IPluginLog log)
    {
        _pluginInterface = pluginInterface;
        _commandManager = commandManager;
        _log = log;
        
        // Initialize configuration
        _configuration = _pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        _configuration.Initialize(_pluginInterface);
        
        // Initialize AT Protocol client
        _atProtocolClient = new AtProtocolClient(_log);
        
        // Initialize account generation service
        _accountGenerationService = new AccountGenerationService(_log, _atProtocolClient);
        
        // Initialize windows
        _mainWindow = new MainWindow(this, _configuration, _atProtocolClient);
        _configWindow = new ConfigWindow(_configuration, _atProtocolClient, _accountGenerationService);
        
        // Register windows with WindowSystem
        WindowSystem.AddWindow(_mainWindow);
        WindowSystem.AddWindow(_configWindow);
        
        // Register UI handlers
        _pluginInterface.UiBuilder.Draw += DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        _pluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        
        // Register chat commands
        _commandManager.AddHandler("/resonance", new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Resonance main window"
        });
        _commandManager.AddHandler("/resonance-config", new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Opens the Resonance configuration window"
        });
        
        // Initialize IPC gates for other plugins to use
        _publishDataGate = _pluginInterface.GetIpcProvider<Dictionary<string, object>, bool>("Resonance.PublishData");
        _authenticateGate = _pluginInterface.GetIpcProvider<string, bool>("Resonance.Authenticate");
        _isAuthenticatedGate = _pluginInterface.GetIpcProvider<bool>("Resonance.IsAuthenticated");
        _getConnectedClientsGate = _pluginInterface.GetIpcProvider<List<string>>("Resonance.GetConnectedClients");
        
        // Events that other plugins can subscribe to
        _dataReceivedGate = _pluginInterface.GetIpcProvider<Dictionary<string, object>, string, object>("Resonance.DataReceived");
        _clientConnectedGate = _pluginInterface.GetIpcProvider<string, object>("Resonance.ClientConnected");
        _clientDisconnectedGate = _pluginInterface.GetIpcProvider<string, object>("Resonance.ClientDisconnected");
        
        // Register IPC handlers
        _publishDataGate.RegisterFunc(PublishData);
        _authenticateGate.RegisterFunc(Authenticate);
        _isAuthenticatedGate.RegisterFunc(IsAuthenticated);
        _getConnectedClientsGate.RegisterFunc(GetConnectedClients);
        
        _log.Info("Resonance initialized - Universal cross-client mod sync ready");
        _log.Info("Supported clients: TeraSync, Neko Net, Anatoli Test, and all Mare forks");
        
        // Show setup window if not configured
        if (!_configuration.IsConfigured && _configuration.ShowSetupWindow)
        {
            _configWindow.IsOpen = true;
        }
    }
    
    /// <summary>
    /// Publishes character/mod data to the AT Protocol network
    /// This is called by Mare forks when they want to sync data
    /// </summary>
    private bool PublishData(Dictionary<string, object> data)
    {
        try
        {
            _log.Debug($"Received data from client for publishing");
            
            if (!_atProtocolClient.IsAuthenticated)
            {
                _log.Warning("Cannot publish data - not authenticated with AT Protocol");
                return false;
            }
            
            // Add user's chosen ResonanceHandle to the data if using auto-account
            if (_configuration.UseAutoAccount && !string.IsNullOrEmpty(_configuration.ResonanceHandle))
            {
                data["ResonanceHandle"] = _configuration.ResonanceHandle;
            }
            
            // Publish to AT Protocol asynchronously
            Task.Run(async () =>
            {
                var success = await _atProtocolClient.PublishCharacterDataAsync(data);
                if (success)
                {
                    _log.Info("Successfully published character data to AT Protocol");
                    // Broadcast to local IPC subscribers with the user's display name
                    var displayName = _configuration.UseAutoAccount ? _configuration.ResonanceHandle : _atProtocolClient.CurrentHandle;
                    _dataReceivedGate.SendMessage(data, displayName ?? "unknown");
                }
                else
                {
                    _log.Error("Failed to publish character data to AT Protocol");
                }
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to publish data");
            return false;
        }
    }
    
    /// <summary>
    /// Authenticates with AT Protocol using a handle and password
    /// </summary>
    private bool Authenticate(string credentials)
    {
        try
        {
            // Parse credentials - expected format: "handle:password"
            var parts = credentials.Split(':', 2);
            if (parts.Length != 2)
            {
                _log.Error("Invalid credentials format. Expected 'handle:password'");
                return false;
            }
            
            var handle = parts[0];
            var password = parts[1];
            
            _log.Info($"Authenticating with handle: {handle}");
            
            // Authenticate asynchronously
            Task.Run(async () =>
            {
                var success = await _atProtocolClient.AuthenticateAsync(handle, password);
                if (success)
                {
                    _log.Info($"Successfully authenticated as: {_atProtocolClient.CurrentHandle}");
                    _clientConnectedGate.SendMessage(_atProtocolClient.CurrentDid ?? handle);
                }
                else
                {
                    _log.Error("Authentication failed");
                }
            });
            
            return true; // Return true for async operation started
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to authenticate");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if currently authenticated to AT Protocol
    /// </summary>
    private bool IsAuthenticated()
    {
        return _atProtocolClient.IsAuthenticated;
    }
    
    /// <summary>
    /// Gets list of currently connected clients (DIDs)
    /// </summary>
    private List<string> GetConnectedClients()
    {
        // TODO: Return actual connected clients from AT Protocol
        return new List<string>();
    }
    
    private void DrawUI() => WindowSystem.Draw();
    
    public void ToggleConfigUI() => _configWindow.Toggle();
    public void ToggleMainUI() => _mainWindow.Toggle();
    
    private void OnCommand(string command, string args) => ToggleMainUI();
    private void OnConfigCommand(string command, string args) => ToggleConfigUI();
    
    public void Dispose()
    {
        // Dispose windows
        WindowSystem.RemoveAllWindows();
        _mainWindow?.Dispose();
        _configWindow?.Dispose();
        
        // Unregister UI handlers
        _pluginInterface.UiBuilder.Draw -= DrawUI;
        _pluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        _pluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
        
        // Unregister commands
        _commandManager.RemoveHandler("/resonance");
        _commandManager.RemoveHandler("/resonance-config");
        
        // Unregister IPC handlers
        _publishDataGate.UnregisterFunc();
        _authenticateGate.UnregisterFunc();
        _isAuthenticatedGate.UnregisterFunc();
        _getConnectedClientsGate.UnregisterFunc();
        
        // Dispose AT Protocol client
        _atProtocolClient?.Dispose();
        
        // Dispose account generation service
        _accountGenerationService?.Dispose();
        
        _log.Info("Resonance disposed");
    }
}