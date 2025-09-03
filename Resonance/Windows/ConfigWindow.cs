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

    public ConfigWindow(Configuration configuration, AtProtocolClient atProtocolClient)
        : base("Resonance Configuration##ConfigWindow", ImGuiWindowFlags.NoCollapse)
    {
        _configuration = configuration;
        _atProtocolClient = atProtocolClient;
        
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
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), FontAwesomeIcon.ExclamationTriangle.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "Setup Required");
        ImGui.Text("Resonance enables cross-client sync between Mare forks.");
        ImGui.Spacing();

        if (!_configuration.IsConfigured)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Status: Not Configured");
            ImGui.Spacing();
            
            ImGui.Text("To use Resonance, you need an AT Protocol handle:");
            ImGui.Bullet(); ImGui.Text("Create a free Bluesky account:");
            ImGui.SameLine();
            if (ImGui.Button("Open bsky.app"))
            {
                Util.OpenLink("https://bsky.app");
            }
            ImGui.Bullet(); ImGui.Text("Your handle will be like: yourname.bsky.social");
            ImGui.Bullet(); ImGui.Text("Enter your credentials below");
            ImGui.Spacing();
            
            var handle = _configuration.AtProtocolHandle;
            if (ImGui.InputText("AT Protocol Handle", ref handle, 200))
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
            
            ImGui.Spacing();
            
            bool canConfigure = !string.IsNullOrEmpty(_configuration.AtProtocolHandle) && 
                               !string.IsNullOrEmpty(_configuration.AtProtocolPassword);
                               
            if (!canConfigure)
                ImGui.BeginDisabled();
                
            if (ImGui.Button("Test Connection & Configure"))
            {
                // Authenticate with AT Protocol asynchronously
                Task.Run(async () =>
                {
                    var success = await _atProtocolClient.AuthenticateAsync(_configuration.AtProtocolHandle, _configuration.AtProtocolPassword);
                    if (success)
                    {
                        _configuration.IsConfigured = true;
                        _configuration.Save();
                    }
                });
            }
            
            if (!canConfigure)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Please enter both handle and password first");
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Status: Configured");
            ImGui.Text($"Handle: {_configuration.AtProtocolHandle}");
            
            if (_atProtocolClient.IsAuthenticated)
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected as: {_atProtocolClient.CurrentHandle}");
                ImGui.Text($"DID: {_atProtocolClient.CurrentDid}");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "Not currently connected");
                if (ImGui.Button("Reconnect"))
                {
                    Task.Run(async () =>
                    {
                        await _atProtocolClient.AuthenticateAsync(_configuration.AtProtocolHandle, _configuration.AtProtocolPassword);
                    });
                }
            }
            
            ImGui.Spacing();
            
            if (ImGui.Button("Reconfigure"))
            {
                _atProtocolClient.Logout();
                _configuration.IsConfigured = false;
                _configuration.AtProtocolHandle = string.Empty;
                _configuration.AtProtocolPassword = string.Empty;
                _configuration.Save();
            }
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