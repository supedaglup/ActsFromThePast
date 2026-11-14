using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePastConfig;

/// <summary>Small helpers for reading the current run: player count, act, room, and whether we're in a legacy act.</summary>
public static class RunInfo
{
    private const string LegacyAssemblyName = "ActsFromThePast";

    private static readonly MethodInfo? DebugGetState = AccessTools.Method(typeof(RunManager), "DebugOnlyGetState");
    private static readonly PropertyInfo? StateProperty = AccessTools.Property(typeof(RunManager), "State");

    /// <summary>The current RunState, or null when no run is loaded.</summary>
    public static RunState? State
    {
        get
        {
            try
            {
                var manager = RunManager.Instance;
                if (manager == null) return null;
                if (DebugGetState != null) return DebugGetState.Invoke(manager, null) as RunState;
                return StateProperty?.GetValue(manager) as RunState;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Total players in the run (1 in single player).</summary>
    public static int PlayerCount(IRunState? runState = null)
    {
        try
        {
            var players = runState?.Players ?? State?.Players;
            var count = players?.Count ?? 1;
            return Math.Max(1, count);
        }
        catch
        {
            return 1;
        }
    }

    public static int CurrentActIndex
    {
        get
        {
            try { return State?.CurrentActIndex ?? 0; }
            catch { return 0; }
        }
    }

    /// <summary>True when the current act was added by Acts from the Past (Exordium, The City, The Beyond).</summary>
    public static bool InLegacyAct
    {
        get
        {
            try
            {
                var act = State?.Act;
                if (act == null) return false;
                var type = act.GetType();
                return type.Assembly.GetName().Name == LegacyAssemblyName
                       || (type.Namespace?.StartsWith(LegacyAssemblyName, StringComparison.Ordinal) ?? false);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Which Slay the Spire 1 act we are in: 1 = Exordium, 2 = The City, 3 = The Beyond, 0 = not a legacy act.</summary>
    public static int LegacyActNumber
    {
        get
        {
            try
            {
                var act = State?.Act;
                if (act == null) return 0;
                var typeName = act.GetType().Name;
                if (!InLegacyAct) return 0;
                if (typeName.Contains("Exordium", StringComparison.OrdinalIgnoreCase)) return 1;
                if (typeName.Contains("City", StringComparison.OrdinalIgnoreCase)) return 2;
                if (typeName.Contains("Beyond", StringComparison.OrdinalIgnoreCase)) return 3;
                return 1;
            }
            catch
            {
                return 0;
            }
        }
    }

    public static bool InShopRoom
    {
        get
        {
            try { return State?.CurrentRoom?.RoomType == RoomType.Shop; }
            catch { return false; }
        }
    }
}
