using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WKSHud = ECommons.UIHelpers.AddonMasterImplementations.AddonMaster.WKSHud;

namespace CENodeCrowdsourcer.Windows;

public class Mission
{
    public string name = "";

    public int jobId = -1;

    public string rank = "A";

    public bool open = true;
}

public class MainWindow : Window, IDisposable
{
    private string JsonPath = "";

    private string Contents = "";

    private JsonFile? Data;

    private float ElapsedTime = 0f;
    private const float Interval = 1f;

    private unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;

    private List<Mission> Missions = [];

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string jsonPath)
        : base("CE Nodes##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        JsonPath = jsonPath;

        var directory = Path.GetDirectoryName(JsonPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(JsonPath))
        {
            File.WriteAllText(JsonPath, "{}");
        }
        else
        {
            // If the file exists but is empty, populate with '{}'
            var info = new FileInfo(JsonPath);
            if (info.Length == 0)
            {
                File.WriteAllText(JsonPath, "{}");
            }
        }

        using StreamReader reader = new(JsonPath);
        Contents = JValue.Parse(reader.ReadToEnd()).ToString(Formatting.Indented);

        foreach (var mission in Svc.Data.GetExcelSheet<WKSMissionUnit>())
        {
            var job = (mission.Unknown1 - 1 == 16) ? "Miner" : "Botanist";
            var name = mission.Item.ToString().Replace(" ", "") + "_" + job;
            if (name == "" || (mission.Unknown1 - 1) < 16)
            {
                continue;
            }

            var rank = "A";
            switch (mission.Unknown17)
            {
                case 1:
                    rank = "D";
                    break;
                case 2:
                    rank = "C";
                    break;
                case 3:
                    rank = "B";
                    break;
            }

            Missions.Add(
                new()
                {
                    name = name,
                    jobId = mission.Unknown1 - 1,
                    rank = rank,
                }
            );
        }

        Data = GetFile() ?? new();
    }

    private JsonFile? GetFile()
    {
        using StreamReader reader = new(JsonPath);
        var json = reader.ReadToEnd();
        return JsonConvert.DeserializeObject<JsonFile>(json);
    }

    private string WriteFile(JsonFile file)
    {
        using StreamWriter writer = new(JsonPath);
        var json = JsonConvert.SerializeObject(file);
        writer.Write(JValue.Parse(json).ToString(Formatting.Indented));

        return JValue.Parse(json).ToString(Formatting.Indented);
    }

    public override void Draw()
    {
        var job = Svc.ClientState.LocalPlayer?.ClassJob.RowId;
        var lastRank = "";
        if (job == null)
        {
            return;
        }

        if (ImGui.BeginChild("Missions scrollable"))
        {
            for (var i = 0; i < Missions.Count; i++)
            {
                var mission = Missions[i];
                if (mission.jobId != job)
                {
                    continue;
                }

                if (mission.rank != lastRank)
                {
                    ImGui.TextUnformatted($"Class: {mission.rank}");
                    lastRank = mission.rank;
                }

                var open = mission.open;
                var count = Data?.Count(mission.name) ?? 0;

                if (count == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.85f, 0.25f, 0.25f, 1.0f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.75f, 0.35f, 1.0f));
                }

                if (
                    ImGui.CollapsingHeader(
                        $"{mission.name} ({count})###{mission.jobId}-{i}",
                        ref open
                    )
                )
                {
                    mission.open = open;

                    if (Data!.missions.TryGetValue(mission.name, out var list))
                    {
                        foreach (var node in list)
                        {
                            ImGui.TextUnformatted(node.Position.ToString());
                        }
                    }
                }

                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Stolen from ICE ;)
    /// https://github.com/LeontopodiumNivale14/Ices-Cosmic-Exploration/blob/1128b4db8967b80a2e6e391bfeabe28b8b75ee3a/ICE/Utilities/Cosmic/CosmicHelpers.cs#L20C43-L22C46
    /// Slightly modified of course...
    /// </summary>
    public static unsafe void OpenStellaMission()
    {
        var info = RaptureAtkUnitManager.Instance()->GetAddonByName("WKSMissionInfomation");

        if (
            GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud)
            && hud.IsAddonReady
            && !(info != null && info->IsVisible && info->IsReady)
        )
        {
            if (EzThrottler.Throttle("Opening Steller Missions"))
            {
                Svc.Log.Debug("Opening Mission Menu");
                hud.Mission();
            }
        }
    }

    public void Tick(IFramework framework)
    {
        ElapsedTime += framework.UpdateDelta.Milliseconds / 1000f;
        if (ElapsedTime < Interval)
        {
            return;
        }

        ElapsedTime -= Interval;

        if (CurrentLunarMission == 0)
        {
            return;
        }

        var nodes = Svc
            .Objects.OrderBy(o =>
                Vector3.Distance(o.Position, Svc.ClientState.LocalPlayer!.Position)
            )
            .Where(o =>
                o.ObjectKind == ObjectKind.GatheringPoint
                && o.Name.GetText() != ""
                && o.IsTargetable
            );

        Data = GetFile() ?? new();

        OpenStellaMission();
        if (
            GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>(
                "WKSMissionInfomation",
                out var info
            ) && info.IsAddonReady
        )
        {
            var job = Svc.ClientState.LocalPlayer?.ClassJob.RowId == 16 ? "Miner" : "Botanist";
            var name = info.Name.Replace(" ", "") + "_" + job;
            foreach (var node in nodes)
            {
                Data.Add(name, new() { Position = node.Position });
            }

            Contents = WriteFile(Data);
        }
    }

    public void Dispose() { }
}
