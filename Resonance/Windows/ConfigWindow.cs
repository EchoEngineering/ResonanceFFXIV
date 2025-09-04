using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Utility;
using Resonance.Services;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Resonance.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly AtProtocolClient _atProtocolClient;
    private readonly AccountGenerationService _accountGenerationService;
    
    private bool _isCreatingAccount = false;
    private string _accountCreationStatus = string.Empty;

    public ConfigWindow(Configuration configuration, AtProtocolClient atProtocolClient, AccountGenerationService accountGenerationService)
        : base("Resonance Configuration##ConfigWindow", ImGuiWindowFlags.NoCollapse)
    {
        _configuration = configuration;
        _atProtocolClient = atProtocolClient;
        _accountGenerationService = accountGenerationService;
        
        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (_configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabs"))
        {
            if (ImGui.BeginTabItem("Setup"))
            {
                DrawSetupTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Settings"))
            {
                DrawSettingsTab();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("About"))
            {
                DrawAboutTab();
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
    }

    private void DrawSetupTab()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), FontAwesomeIcon.Rocket.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), "Cross-Client Sync Setup");
        ImGui.Text("Enable sync between different Mare forks (TeraSync ↔ Neko Net ↔ etc.)");
        ImGui.Spacing();

        if (!_configuration.IsConfigured)
        {
            DrawEasySetupSection();
            ImGui.Spacing();
            DrawAdvancedSetupSection();
        }
        else
        {
            DrawConfiguredSection();
        }
    }
    
    private void DrawEasySetupSection()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(new Vector4(0, 1, 0, 1), FontAwesomeIcon.Star.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text("Recommended: Easy Setup");
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1), "No social media accounts required!");
        ImGui.Spacing();
        
        ImGui.Text("Choose your sync display name:");
        var resonanceHandle = _configuration.ResonanceHandle;
        if (ImGui.InputText("Your Display Name", ref resonanceHandle, 30))
        {
            if (_accountGenerationService.IsValidResonanceHandle(resonanceHandle))
            {
                _configuration.ResonanceHandle = resonanceHandle;
                _configuration.Save();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("This is how other users will see you (e.g., 'MyCharacterName')");
            
        // Show what their actual handle will be
        if (!string.IsNullOrEmpty(resonanceHandle))
        {
            var previewHandle = resonanceHandle.ToLowerInvariant().Replace(" ", "-");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"Your handle will be: {previewHandle}.sync.terasync.app");
        }
            
        if (!_accountGenerationService.IsValidResonanceHandle(resonanceHandle) && !string.IsNullOrEmpty(resonanceHandle))
        {
            if (resonanceHandle.Length > 30)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Name too long ({resonanceHandle.Length}/30 characters). Please shorten.");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Invalid name. Use letters, numbers, spaces, - or _ only");
            }
        }
        
        bool canEnable = _accountGenerationService.IsValidResonanceHandle(resonanceHandle) && 
                        !string.IsNullOrEmpty(resonanceHandle);
        
        if (!canEnable || _isCreatingAccount)
            ImGui.BeginDisabled();
            
        if (ImGui.Button("Enable Cross-Client Sync"))
        {
            _isCreatingAccount = true;
            _accountCreationStatus = "Creating anonymous account...";
            
            Task.Run(async () =>
            {
                var (success, handle, password, errorMessage) = await _accountGenerationService.CreateAutoAccountAsync();
                
                _isCreatingAccount = false;
                
                if (success)
                {
                    _configuration.AutoGeneratedHandle = handle;
                    _configuration.AutoGeneratedPassword = password;
                    _configuration.UseAutoAccount = true;
                    _configuration.Save();
                    
                    // Try to authenticate
                    var authSuccess = await _atProtocolClient.AuthenticateAsync(handle, password);
                    if (authSuccess)
                    {
                        _configuration.IsConfigured = true;
                        _configuration.Save();
                        _accountCreationStatus = "Success! Cross-client sync enabled.";
                    }
                    else
                    {
                        _accountCreationStatus = "Account created but authentication failed.";
                    }
                }
                else
                {
                    _accountCreationStatus = $"Setup failed: {errorMessage}";
                    
                    // If it's a verification error, provide helpful guidance
                    if (errorMessage.Contains("Phone verification") || errorMessage.Contains("Verification is now required"))
                    {
                        _accountCreationStatus += "\n\nBluesky now requires phone verification for new accounts. Please:\n1. Create a free Bluesky account at bsky.app\n2. Use the Advanced Setup below to connect your account";
                    }
                }
            });
        }
        
        if (!canEnable || _isCreatingAccount)
        {
            ImGui.EndDisabled();
            if (!canEnable && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Please enter a valid display name first");
        }
        
        if (_isCreatingAccount)
        {
            ImGui.SameLine();
            ImGui.Text("Please wait...");
        }
        
        if (!string.IsNullOrEmpty(_accountCreationStatus))
        {
            var color = _accountCreationStatus.Contains("Success") ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1);
            ImGui.TextColored(color, _accountCreationStatus);
        }
    }
    
    private void DrawAdvancedSetupSection()
    {
        if (ImGui.CollapsingHeader("Advanced: Use Existing Bluesky Account"))
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "Connect your existing Bluesky account to sync with the broader AT Protocol network:");
            ImGui.Spacing();
            
            ImGui.Text("Create a free Bluesky account:");
            ImGui.SameLine();
            if (ImGui.Button("Open bsky.app"))
            {
                Util.OpenLink("https://bsky.app");
            }
            
            var handle = _configuration.AtProtocolHandle;
            if (ImGui.InputText("Bluesky Handle", ref handle, 200))
            {
                _configuration.AtProtocolHandle = handle;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Your Bluesky handle (e.g., alice.bsky.social)");
            
            var password = _configuration.AtProtocolPassword;
            if (ImGui.InputText("Password", ref password, 200, ImGuiInputTextFlags.Password))
            {
                _configuration.AtProtocolPassword = password;
                _configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Your Bluesky password (stored locally only)");
            
            bool canManualConfigure = !string.IsNullOrEmpty(_configuration.AtProtocolHandle) && 
                                     !string.IsNullOrEmpty(_configuration.AtProtocolPassword);
                                     
            if (!canManualConfigure)
                ImGui.BeginDisabled();
                
            if (ImGui.Button("Connect with Bluesky Account"))
            {
                Task.Run(async () =>
                {
                    var success = await _atProtocolClient.AuthenticateAsync(_configuration.AtProtocolHandle, _configuration.AtProtocolPassword);
                    if (success)
                    {
                        _configuration.UseAutoAccount = false;
                        _configuration.IsConfigured = true;
                        _configuration.Save();
                    }
                });
            }
            
            if (!canManualConfigure)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Please enter both handle and password first");
            }
        }
    }
    
    private void DrawConfiguredSection()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(new Vector4(0, 1, 0, 1), FontAwesomeIcon.CheckCircle.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Cross-Client Sync Enabled");
        
        if (_configuration.UseAutoAccount)
        {
            ImGui.Text($"Display Name: {_configuration.ResonanceHandle}");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Using self-hosted anonymous account");
        }
        else
        {
            ImGui.Text($"Bluesky Handle: {_configuration.AtProtocolHandle}");
        }
        
        if (_atProtocolClient.IsAuthenticated)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected as: {_atProtocolClient.CurrentHandle}");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), $"DID: {_atProtocolClient.CurrentDid}");
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "Not currently connected");
            if (ImGui.Button("Reconnect"))
            {
                Task.Run(async () =>
                {
                    var handle = _configuration.UseAutoAccount ? _configuration.AutoGeneratedHandle : _configuration.AtProtocolHandle;
                    var password = _configuration.UseAutoAccount ? _configuration.AutoGeneratedPassword : _configuration.AtProtocolPassword;
                    await _atProtocolClient.AuthenticateAsync(handle, password);
                });
            }
        }
        
        ImGui.Spacing();
        
        if (ImGui.Button("Reset Configuration"))
        {
            _atProtocolClient.Logout();
            _configuration.IsConfigured = false;
            _configuration.UseAutoAccount = true;
            _configuration.ResonanceHandle = string.Empty;
            _configuration.AutoGeneratedHandle = string.Empty;
            _configuration.AutoGeneratedPassword = string.Empty;
            _configuration.AtProtocolHandle = string.Empty;
            _configuration.AtProtocolPassword = string.Empty;
            _configuration.Save();
            _accountCreationStatus = string.Empty;
        }
    }

    private void DrawSettingsTab()
    {
        ImGui.Text("General Settings");
        ImGui.Separator();
        
        var movable = _configuration.IsConfigWindowMovable;
        if (ImGui.Checkbox("Allow moving config window", ref movable))
        {
            _configuration.IsConfigWindowMovable = movable;
            _configuration.Save();
        }
        
        var debug = _configuration.EnableDebugLogging;
        if (ImGui.Checkbox("Enable debug logging", ref debug))
        {
            _configuration.EnableDebugLogging = debug;
            _configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Logs detailed information for troubleshooting");
        
        var welcome = _configuration.ShowWelcomeMessage;
        if (ImGui.Checkbox("Show welcome message on startup", ref welcome))
        {
            _configuration.ShowWelcomeMessage = welcome;
            _configuration.Save();
        }
    }

    private void DrawAboutTab()
    {
        ImGui.Text("Resonance - Universal FFXIV Mod Sync Protocol");
        ImGui.Text("Version 1.0.0.0");
        ImGui.Spacing();
        
        ImGui.Text("Enables cross-client synchronization between:");
        ImGui.Bullet(); ImGui.Text("TeraSync");
        ImGui.Bullet(); ImGui.Text("Neko Net");
        ImGui.Bullet(); ImGui.Text("Other Mare-compatible clients");
        ImGui.Spacing();
        
        ImGui.Text("Technology:");
        ImGui.Bullet(); ImGui.Text("AT Protocol (Bluesky's decentralized protocol)");
        ImGui.Bullet(); ImGui.Text("IPC communication with Mare clients");
        ImGui.Bullet(); ImGui.Text("Universal character data format");
        ImGui.Spacing();
        
        if (ImGui.Button("GitHub Repository"))
        {
            Util.OpenLink("https://github.com/EchoEngineering/ResonanceFFXIV");
        }
        ImGui.SameLine();
        if (ImGui.Button("Report Issue"))
        {
            Util.OpenLink("https://github.com/EchoEngineering/ResonanceFFXIV/issues");
        }
        
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Licensed under AGPL-3.0");
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "© 2025 EchoEngineering");
    }
}