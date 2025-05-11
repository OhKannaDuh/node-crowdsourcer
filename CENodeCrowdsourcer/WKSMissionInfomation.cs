using Dalamud.Memory;
using ECommons;
using Base = ECommons.UIHelpers.AddonMasterImplementations.AddonMaster.WKSMissionInfomation;

namespace CENodeCrowdsourcer;

public unsafe partial class WKSMissionInfomation : Base
{
    public WKSMissionInfomation(nint addon)
        : base(addon) { }

    public WKSMissionInfomation(void* addon)
        : base(addon) { }

    public string Name
    {
        get
        {
            return MemoryHelper
                .ReadSeStringNullTerminated((nint)Addon->AtkValues[0].String.Value)
                .GetText();
        }
    }
}
