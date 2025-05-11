using System.IO;
using CENodeCrowdsourcer.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;

namespace CENodeCrowdsourcer;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private const string CommandName = "/cenodes";

    public readonly WindowSystem WindowSystem = new("CENodeCrowdsourcer");
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this, Module.ObjectFunctions);

        var jsonPath = Path.Combine(PluginInterface.ConfigDirectory.FullName, "output.json");

        MainWindow = new MainWindow(this, jsonPath);

        WindowSystem.AddWindow(MainWindow);

        Svc.Commands.AddHandler(
            CommandName,
            new CommandInfo(OnCommand) { HelpMessage = "Open nodes window" }
        );

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Svc.Framework.Update += MainWindow.Tick;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        Svc.Framework.Update -= MainWindow.Tick;
        MainWindow.Dispose();

        ECommonsMain.Dispose();

        Svc.Commands.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleMainUI() => MainWindow.Toggle();
}
