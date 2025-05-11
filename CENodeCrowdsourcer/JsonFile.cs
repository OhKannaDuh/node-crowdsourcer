using System.Collections.Generic;
using ECommons.DalamudServices;

namespace CENodeCrowdsourcer;

public class JsonFile
{
    public Dictionary<string, List<Entry>> missions = [];

    public void Add(string missionName, Entry entry)
    {
        if (!missions.TryGetValue(missionName, out var list))
        {
            list = new List<Entry>();
            missions[missionName] = list;
        }

        if (!list.Contains(entry))
        {
            Svc.Log.Info("Adding node to " + missionName);
            list.Add(entry);
            Svc.Log.Debug("Nodes in pool " + list.Count);
        }
    }

    public int Count(string missionName)
    {
        return missions.TryGetValue(missionName, out var list) ? list.Count : 0;
    }
}
