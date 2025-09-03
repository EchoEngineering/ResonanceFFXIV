using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Resonance;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Resonance";
    
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IPluginLog _log;
    
    // IPC Providers - What Resonance offers to other plugins
    private readonly ICallGateProvider<Dictionary<string, object>, bool> _publishDataGate;
    private readonly ICallGateProvider<string, bool> _authenticateGate;
    private readonly ICallGateProvider<bool> _isAuthenticatedGate;
    private readonly ICallGateProvider<List<string>> _getConnectedClientsGate;
    
    // IPC Subscribers - Events Resonance sends to other plugins
    private readonly ICallGateProvider<Dictionary<string, object>, string, object> _dataReceivedGate;
    private readonly ICallGateProvider<string, object> _clientConnectedGate;
    private readonly ICallGateProvider<string, object> _clientDisconnectedGate;
    
    public Plugin(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        _pluginInterface = pluginInterface;
        _log = log;
        
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
        _log.Info("Supported clients: TeraSync, Neko Net, Lightless, Snowcloak, and all Mare forks");
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
            
            // TODO: Convert to AT Protocol record and publish
            // For now, just echo back to all connected clients for testing
            Task.Run(async () =>
            {
                await Task.Delay(100); // Simulate network delay
                
                // Broadcast to all IPC subscribers
                _dataReceivedGate.SendMessage(data, "test-user-did");
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
    /// Authenticates with AT Protocol using a handle
    /// </summary>
    private bool Authenticate(string handle)
    {
        try
        {
            _log.Info($"Authenticating with handle: {handle}");
            
            // TODO: Implement AT Protocol authentication
            // For now, always return true for testing
            
            Task.Run(async () =>
            {
                await Task.Delay(100);
                _clientConnectedGate.SendMessage(handle);
            });
            
            return true;
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
        // TODO: Check actual authentication status
        return false;
    }
    
    /// <summary>
    /// Gets list of currently connected clients (DIDs)
    /// </summary>
    private List<string> GetConnectedClients()
    {
        // TODO: Return actual connected clients from AT Protocol
        return new List<string>();
    }
    
    public void Dispose()
    {
        _publishDataGate.UnregisterFunc();
        _authenticateGate.UnregisterFunc();
        _isAuthenticatedGate.UnregisterFunc();
        _getConnectedClientsGate.UnregisterFunc();
        
        _log.Info("Resonance disposed");
    }
}