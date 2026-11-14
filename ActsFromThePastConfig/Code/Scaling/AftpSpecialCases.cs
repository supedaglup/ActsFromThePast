using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePastConfig.Scaling;

/// <summary>
/// Two Acts from the Past enemies do their own co-op scaling instead of going through the game's HP scaling,
/// so the regular HP patch never sees them. Found by disassembling the Workshop build (v1.0.5):
///
///  * Awakened One, phase 2: max HP is set to  Phase2Hp x playerCount  when it is reborn.
///  * Transient: its HP doesn't matter (it fades away), so the mod scales its ATTACK DAMAGE by
///    playerCount x the game's act scaling instead - exactly the factor the game uses for HP.
///
/// Both are adjusted by the same ratio as your HP slider: (your HP multiplier) / (the base game's HP multiplier).
/// With the sliders at their defaults that ratio is 1, so nothing changes unless you move the HP slider.
/// </summary>
public static class AftpSpecialCases
{
    /// <summary>Your HP multiplier divided by the base game's, for this player count / act. 1 = no change.</summary>
    internal static decimal HpRatio(int playerCount, EncounterModel? encounter, int actIndex)
    {
        if (playerCount <= 1 || !ScalingSettings.Applies) return 1m;

        var vanillaAct = ScalingPatches.VanillaActScaling(encounter, actIndex);
        if (vanillaAct is null || vanillaAct <= 0m) return 1m;

        var vanillaTotal = playerCount * vanillaAct.Value;
        var ours = ScalingSettings.ToMultiplier(ScalingSettings.Current.For(playerCount).Hp)
                   * ScalingPatches.Progression(encounter, actIndex);
        return ours / vanillaTotal;
    }

    private static (int PlayerCount, EncounterModel? Encounter, int ActIndex) Context(MonsterModel monster)
    {
        var combat = monster.Creature?.CombatState;
        if (combat == null) return (RunInfo.PlayerCount(), null, RunInfo.CurrentActIndex);
        int actIndex;
        try { actIndex = combat.RunState.CurrentActIndex; }
        catch { actIndex = RunInfo.CurrentActIndex; }
        return (Math.Max(1, combat.Players.Count), combat.Encounter, actIndex);
    }

    private static MethodBase? Getter(string typeName, string property)
    {
        var type = AccessTools.TypeByName(typeName);
        return type == null ? null : AccessTools.PropertyGetter(type, property);
    }

    // ───────────────────────── Awakened One phase 2 HP ─────────────────────────

    [HarmonyPatch]
    public static class AwakenedOnePhase2Hp
    {
        private const string TypeName = "ActsFromThePast.Acts.TheBeyond.Enemies.AwakenedOne";

        public static bool Prepare()
        {
            var ok = Getter(TypeName, "Phase2Hp") != null;
            if (!ok) ModMain.Logger.Warn("AwakenedOne.Phase2Hp not found; Awakened One phase 2 HP will use Acts from the Past's own scaling.");
            return ok;
        }

        public static MethodBase? TargetMethod() => Getter(TypeName, "Phase2Hp");

        public static void Postfix(object __instance, ref int __result)
        {
            try
            {
                if (__instance is not MonsterModel monster) return;
                var (players, encounter, act) = Context(monster);
                var ratio = HpRatio(players, encounter, act);
                if (ratio == 1m) return;
                __result = (int)Math.Max(1m, ScalingPatches.RoundAmount(__result * ratio));
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Awakened One HP patch failed: {e}");
            }
        }
    }

    // ───────────────────────── Transient attack damage ─────────────────────────

    [HarmonyPatch]
    public static class TransientDamage
    {
        private const string TypeName = "ActsFromThePast.Acts.TheBeyond.Enemies.Transient";

        public static bool Prepare()
        {
            var ok = Getter(TypeName, "CurrentAttackDamage") != null;
            if (!ok) ModMain.Logger.Warn("Transient.CurrentAttackDamage not found; Transient damage will use Acts from the Past's own scaling.");
            return ok;
        }

        public static MethodBase? TargetMethod() => Getter(TypeName, "CurrentAttackDamage");

        public static void Postfix(object __instance, ref int __result)
        {
            try
            {
                if (__instance is not MonsterModel monster) return;
                var (players, encounter, act) = Context(monster);
                if (players <= 1) return;

                var ratio = HpRatio(players, encounter, act);
                if (ratio == 1m) return;

                // Rebuild the number from its parts so integer truncation happens once, like the original:
                //   (StartingDeathDmg + Count * IncrementDmg) * multiplier
                var t = Traverse.Create(__instance);
                var start = t.Property("StartingDeathDmg").GetValue<int>();
                var count = t.Property("Count").GetValue<int>();
                var increment = Convert.ToInt32(t.Field("IncrementDmg").GetValue());
                var aftpMultiplier = t.Field("_multiplayerDamageMultiplier").GetValue<decimal>();

                __result = (int)((start + count * increment) * aftpMultiplier * ratio);
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Transient damage patch failed: {e}");
            }
        }
    }
}
