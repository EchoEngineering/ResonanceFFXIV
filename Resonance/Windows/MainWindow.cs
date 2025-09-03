using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System;
using System.Numerics;

namespace Resonance.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly Configuration _configuration;

    public MainWindow(Plugin plugin, Configuration configuration)
        : base("Resonance - Universal Sync###MainWindow")
    {
        _plugin = plugin;
        _configuration = configuration;
        
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        DrawStatusSection();
        ImGui.Separator();
        DrawConnectionSection();
        ImGui.Separator();
        DrawCompatibilitySection();
        
        if (_configuration.ShowWelcomeMessage)
        {
            ImGui.Separator();
            DrawWelcomeSection();
        }
    }

    private void DrawStatusSection()
    {
        ImGui.Text("Status");
        
        if (!_configuration.IsConfigured)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), FontAwesomeIcon.ExclamationCircle.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Not Configured");
            ImGui.SameLine();
            if (ImGui.Button("Setup"))
            {
                _plugin.ToggleConfigUI();
            }
            
            ImGui.TextWrapped("Resonance needs to be configured before it can enable cross-client sync. Click Setup to get started.");
        }
        else
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(new Vector4(0, 1, 0, 1), FontAwesomeIcon.CheckCircle.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Configured");
            ImGui.Text($"Handle: {_configuration.AtProtocolHandle}");
            
            // TODO: Show actual connection status when AT Protocol is implemented
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "AT Protocol: Not Connected (Not Implemented Yet)");
        }
    }

    private void DrawConnectionSection()
    {
        ImGui.Text("Mare Client Detection");
        
        // TODO: Implement actual detection of Mare clients
        bool teraSyncDetected = CheckForClient("TeraSync");
        bool nekoNetDetected = CheckForClient("NekoNet");
        
        DrawClientStatus("TeraSync", teraSyncDetected);
        DrawClientStatus("Neko Net", nekoNetDetected);
        DrawClientStatus("Other Mare Clients", false);
        
        if (!teraSyncDetected && !nekoNetDetected)
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "No compatible Mare clients detected.");
            ImGui.TextWrapped("Resonance works alongside Mare clients like TeraSync, Neko Net, etc. Install a Mare client to enable sync functionality.");
        }
    }

    private void DrawClientStatus(string clientName, bool detected)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (detected)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), FontAwesomeIcon.CheckCircle.ToIconString());
        }
        else
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), FontAwesomeIcon.Circle.ToIconString());
        }
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text(clientName);
    }

    private void DrawCompatibilitySection()
    {
        ImGui.Text("Sync Capabilities");
        
        if (ImGui.BeginTable("SyncCapabilities", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Feature", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableHeadersRow();
            
            AddCapabilityRow("Glamourer Data", "Ready", new Vector4(0, 1, 0, 1), FontAwesomeIcon.Check);
            AddCapabilityRow("File Replacements", "Ready", new Vector4(0, 1, 0, 1), FontAwesomeIcon.Check);
            AddCapabilityRow("Moodles Status", "Ready", new Vector4(0, 1, 0, 1), FontAwesomeIcon.Check);
            AddCapabilityRow("SimpleHeels", "Ready", new Vector4(0, 1, 0, 1), FontAwesomeIcon.Check);
            AddCapabilityRow("Customize+", "Ready", new Vector4(0, 1, 0, 1), FontAwesomeIcon.Check);
            AddCapabilityRow("AT Protocol Sync", "In Progress", new Vector4(1, 0.8f, 0, 1), FontAwesomeIcon.Cog);
            
            ImGui.EndTable();
        }
    }

    private void AddCapabilityRow(string feature, string status, Vector4 statusColor, FontAwesomeIcon icon)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(feature);
        
        ImGui.TableNextColumn();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(statusColor, icon.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(statusColor, status);
    }

    private void DrawWelcomeSection()
    {
        ImGui.TextColored(new Vector4(0, 0.8f, 1, 1), "Welcome to Resonance!");
        ImGui.TextWrapped("Resonance enables different Mare clients to sync with each other. Your TeraSync can now sync with someone using Neko Net!");
        
        ImGui.Spacing();
        ImGui.Text("What you need to know:");
        ImGui.Bullet(); ImGui.TextWrapped("Resonance works alongside your existing Mare client (TeraSync, Neko Net, etc.)");
        ImGui.Bullet(); ImGui.TextWrapped("Both you and your sync partner need Resonance installed");
        ImGui.Bullet(); ImGui.TextWrapped("You'll need a free Bluesky account for the decentralized sync network");
        ImGui.Bullet(); ImGui.TextWrapped("Your existing sync functionality remains unchanged");
        
        ImGui.Spacing();
        
        if (ImGui.Button("Hide Welcome Message"))
        {
            _configuration.ShowWelcomeMessage = false;
            _configuration.Save();
        }
    }

    private bool CheckForClient(string clientName)
    {
        // TODO: Implement actual IPC detection of other Mare clients
        // For now, return false since AT Protocol isn't implemented yet
        return false;
    }
}