using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePastConfig.Scaling;

/// <summary>
/// The patches that actually change enemy numbers. Each one:
///  1. checks that we are allowed to act (legacy act / everywhere, see <see cref="ScalingSettings.Applies"/>),
///  2. looks up the slider values for the current player count,
///  3. rewrites the number the base game produced.
///
/// How the base game scales co-op enemies (for reference):
///   HP     = base * playerCount * actScaling        (actScaling: 1.1 / 1.2 / 1.3)
///   Block  = base * playerCount * actScaling        (2 players: flat x2)
///   Buffs  = base * playerCount * actScaling        (only powers flagged ShouldScaleInMultiplayer; a few specials)
///   Damage = base                                   (never scaled)
/// </summary>
public static class ScalingPatches
{
    // ────────────────────────── vanilla act scaling (for "scale up in later acts") ──────────────────────────

    private static MethodInfo? _getMultiplayerScaling;
    private static object? _scalingModelInstance;
    [ThreadStatic] private static bool _insideVanillaCall;
    private static bool _warnedNoInstance;

    /// <summary>Asks the base game for its own act scaling factor (1.1 / 1.2 / 1.3 ...). Null if unavailable.</summary>
    internal static decimal? VanillaActScaling(EncounterModel? encounter, int actIndex)
    {
        if (_getMultiplayerScaling == null) return null;
        if (!_getMultiplayerScaling.IsStatic && _scalingModelInstance == null)
        {
            if (!_warnedNoInstance)
            {
                _warnedNoInstance = true;
                ModMain.Logger.Warn("Act scaling model not seen yet; using flat values until the first co-op fight starts.");
            }
            return null;
        }

        try
        {
            _insideVanillaCall = true;
            var result = _getMultiplayerScaling.Invoke(
                _getMultiplayerScaling.IsStatic ? null : _scalingModelInstance,
                new object?[] { encounter, actIndex });
            return result switch
            {
                decimal d => d,
                double d => (decimal)d,
                float f => (decimal)f,
                int i => i,
                _ => null,
            };
        }
        catch (Exception e)
        {
            ModMain.Logger.Warn($"Could not read the base game's act scaling: {e.Message}");
            return null;
        }
        finally
        {
            _insideVanillaCall = false;
        }
    }

    /// <summary>
    /// How much higher than Act 1 the base game would scale right now (1.0 in Act 1, ~1.09 in Act 2, ...).
    /// Returns 1 when "Scale up slightly in later acts" is off.
    /// </summary>
    internal static decimal Progression(EncounterModel? encounter, int actIndex)
    {
        if (!ScalingSettings.Current.ScaleUpInLaterActs) return 1m;
        if (actIndex <= 0) return 1m;

        var now = VanillaActScaling(encounter, actIndex);
        var first = VanillaActScaling(encounter, 0);
        if (now is null || first is null || first <= 0m || now <= 0m) return 1m;
        return now.Value / first.Value;
    }

    private static EncounterModel? CurrentEncounter()
    {
        try
        {
            var combat = CombatManager.Instance;
            if (combat == null) return null;
            var getState = AccessTools.Method(typeof(CombatManager), "DebugOnlyGetState");
            var state = getState?.Invoke(combat, null);
            if (state == null) return null;
            return Traverse.Create(state).Property("Encounter").GetValue() as EncounterModel;
        }
        catch
        {
            return null;
        }
    }

    internal static decimal RoundAmount(decimal value) => Math.Round(value, MidpointRounding.AwayFromZero);

    // ────────────────────────── manual patches (shape checked at runtime) ──────────────────────────

    internal static void ApplyManualPatches(Harmony harmony)
    {
        // 1. Observe MultiplayerScalingModel.GetMultiplayerScaling so we can ask it for act 1 vs. act N values.
        try
        {
            _getMultiplayerScaling = AccessTools.Method(typeof(MultiplayerScalingModel), "GetMultiplayerScaling");
            if (_getMultiplayerScaling == null || _getMultiplayerScaling.GetParameters().Length != 2)
            {
                ModMain.Logger.Warn("MultiplayerScalingModel.GetMultiplayerScaling not found or changed shape; 'scale up in later acts' will be flat.");
                _getMultiplayerScaling = null;
            }
            else if (!_getMultiplayerScaling.IsStatic)
            {
                harmony.Patch(_getMultiplayerScaling,
                    postfix: new HarmonyMethod(typeof(ScalingPatches), nameof(CaptureScalingModelInstance)));
                ModMain.Logger.Info("Patched: MultiplayerScalingModel.GetMultiplayerScaling (capture)");
            }
        }
        catch (Exception e)
        {
            ModMain.Logger.Error($"Failed to patch GetMultiplayerScaling: {e}");
            _getMultiplayerScaling = null;
        }

        // 2. Creature.ScaleHpForMultiplayer(decimal hp, EncounterModel encounter, int playerCount, int actIndex)
        try
        {
            var method = AccessTools.Method(typeof(Creature), "ScaleHpForMultiplayer");
            if (method == null) throw new MissingMethodException("Creature.ScaleHpForMultiplayer not found");

            var ps = method.GetParameters();
            _hpArgIndex = Array.FindIndex(ps, p => p.ParameterType == typeof(decimal));
            _encounterArgIndex = Array.FindIndex(ps, p => typeof(EncounterModel).IsAssignableFrom(p.ParameterType));
            var intIndices = ps.Select((p, i) => (p, i)).Where(t => t.p.ParameterType == typeof(int)).Select(t => t.i).ToArray();
            _playerCountArgIndex = intIndices.Length > 0 ? intIndices[0] : -1;
            _actIndexArgIndex = intIndices.Length > 1 ? intIndices[1] : -1;

            if (_hpArgIndex < 0 || _playerCountArgIndex < 0)
                throw new MissingMethodException($"Creature.ScaleHpForMultiplayer has an unexpected signature: {method}");

            harmony.Patch(method, postfix: new HarmonyMethod(typeof(ScalingPatches), nameof(ScaleHpForMultiplayer_Postfix)));
            ModMain.Logger.Info("Patched: Creature.ScaleHpForMultiplayer");
        }
        catch (Exception e)
        {
            ModMain.Logger.Error($"Failed to patch enemy HP scaling: {e}");
        }
    }

    public static void CaptureScalingModelInstance(object __instance)
    {
        if (_insideVanillaCall) return;
        _scalingModelInstance ??= __instance;
    }

    // ────────────────────────── HP ──────────────────────────

    private static int _hpArgIndex = -1, _encounterArgIndex = -1, _playerCountArgIndex = -1, _actIndexArgIndex = -1;

    /// <summary>Co-op HP (2+ players). The game calls this with the un-scaled HP and returns the scaled HP.</summary>
    public static void ScaleHpForMultiplayer_Postfix(object[] __args, ref decimal __result)
    {
        try
        {
            if (!ScalingSettings.Applies) return;

            var hp = (decimal)__args[_hpArgIndex];
            var playerCount = (int)__args[_playerCountArgIndex];
            var actIndex = _actIndexArgIndex >= 0 ? (int)__args[_actIndexArgIndex] : RunInfo.CurrentActIndex;
            var encounter = _encounterArgIndex >= 0 ? __args[_encounterArgIndex] as EncounterModel : null;
            if (hp <= 0m || playerCount <= 1) return;

            var stats = ScalingSettings.Current.For(playerCount);
            var target = ScalingSettings.ToMultiplier(stats.Hp) * Progression(encounter, actIndex);

            __result = Math.Max(1m, RoundAmount(hp * target));
        }
        catch (Exception e)
        {
            ModMain.Logger.Error($"HP scaling patch failed: {e}");
        }
    }

    /// <summary>Solo HP. In single player the game skips co-op scaling entirely, so we apply the solo slider here.</summary>
    [HarmonyPatch(typeof(Creature), nameof(Creature.ScaleMonsterHpForMultiplayer))]
    public static class SoloHpPatch
    {
        public static void Postfix(Creature __instance, int playerCount)
        {
            try
            {
                if (playerCount > 1) return;
                if (!ScalingSettings.Applies) return;

                var mult = ScalingSettings.ToMultiplier(ScalingSettings.Current.Solo.Hp);
                if (mult == 1m) return;

                decimal maxHp = Math.Max(1m, RoundAmount(__instance.MaxHp * mult));
                __instance.SetMaxHpInternal(maxHp);
                __instance.SetCurrentHpInternal(maxHp);
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Solo HP patch failed: {e}");
            }
        }
    }

    // ────────────────────────── Block ──────────────────────────

    /// <summary>The game returns a multiplier for enemy block (1 = no change). We replace it with ours.</summary>
    [HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.ModifyBlockMultiplicative))]
    public static class BlockPatch
    {
        public static void Postfix(ref decimal __result, Creature target)
        {
            try
            {
                if (target == null || target.Side != CombatSide.Enemy) return;
                if (!ScalingSettings.Applies) return;

                var playerCount = RunInfo.PlayerCount();
                var stats = ScalingSettings.Current.For(playerCount);

                if (playerCount <= 1)
                {
                    var mult = ScalingSettings.ToMultiplier(stats.Block);
                    if (mult != 1m) __result *= mult;
                    return;
                }

                // __result == 1 means the game chose not to scale this block (e.g. not a monster move); respect that.
                if (__result == 1m) return;

                __result = ScalingSettings.ToMultiplier(stats.Block) * Progression(CurrentEncounter(), RunInfo.CurrentActIndex);
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Block scaling patch failed: {e}");
            }
        }
    }

    // ────────────────────────── Enemy buffs ──────────────────────────

    /// <summary>
    /// The game scales certain enemy buffs in co-op through PowerModel.GetScaledAmountForMultiplayer (only for powers
    /// flagged ShouldScaleInMultiplayer, only on enemies, only with 2+ players). A few powers override it with their
    /// own formula (Artifact +1 per player, Plating, Slippery, Skittish, Buffer). We keep the game's choice of WHICH
    /// buffs scale and HOW, and only shrink or grow the extra amount the game added, by the same proportion as your
    /// slider compared to the game's normal factor (playerCount x act scaling). Defaults = no change.
    /// </summary>
    [HarmonyPatch]
    public static class BuffPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var found = new List<MethodBase>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).Cast<Type>().ToArray(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type == null || !typeof(PowerModel).IsAssignableFrom(type)) continue;
                    var method = type.GetMethod("GetScaledAmountForMultiplayer",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (method != null && !method.IsAbstract && method.GetMethodBody() != null) found.Add(method);
                }
            }
            ModMain.Logger.Info($"Enemy buff scaling: patching {found.Count} GetScaledAmountForMultiplayer method(s).");
            return found;
        }

        public static void Postfix(ICombatState combatState, decimal amount, ref decimal __result)
        {
            try
            {
                if (amount <= 0m || __result == amount) return;
                if (!ScalingSettings.Applies) return;

                var playerCount = RunInfo.PlayerCount(combatState?.RunState);
                if (playerCount <= 1) return;

                EncounterModel? encounter = null;
                var actIndex = RunInfo.CurrentActIndex;
                try
                {
                    encounter = combatState?.Encounter;
                    if (combatState?.RunState != null) actIndex = combatState.RunState.CurrentActIndex;
                }
                catch { /* use defaults */ }

                var vanillaAct = VanillaActScaling(encounter, actIndex);
                if (vanillaAct is null || vanillaAct <= 0m) return;
                var vanillaGeneral = playerCount * vanillaAct.Value;
                if (vanillaGeneral <= 1m) return;

                var stats = ScalingSettings.Current.For(playerCount);
                var wanted = ScalingSettings.ToMultiplier(stats.Buffs) * Progression(encounter, actIndex);
                var fraction = Math.Max(0m, (wanted - 1m) / (vanillaGeneral - 1m));
                if (fraction == 1m) return;

                __result = Math.Max(1m, RoundAmount(amount + (__result - amount) * fraction));
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Buff scaling patch failed: {e}");
            }
        }
    }

    // ────────────────────────── Enemy attack damage ──────────────────────────

    /// <summary>Base damage of enemy attacks, before Strength/Vulnerable etc. are added (also drives the intent number).</summary>
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
    public static class DamagePatch
    {
        public static void Prefix(IRunState runState, Creature? dealer, ref decimal damage, ValueProp props)
        {
            try
            {
                if (dealer == null || dealer.Side != CombatSide.Enemy) return;
                if (!props.HasFlag(ValueProp.Move)) return;
                if (!ScalingSettings.Applies) return;

                var stats = ScalingSettings.Current.For(RunInfo.PlayerCount(runState));
                var mult = ScalingSettings.ToMultiplier(stats.Damage);
                if (mult != 1m) damage *= mult;
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Damage scaling patch failed: {e}");
            }
        }
    }

    // ────────────────────────── Debuffs on players ──────────────────────────

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountReceived))]
    public static class DebuffPatch
    {
        public static void Postfix(ICombatState combatState, PowerModel canonicalPower, Creature target, Creature? giver, ref decimal __result)
        {
            try
            {
                if (target == null || canonicalPower == null) return;
                if (__result <= 0m) return;
                if (!ScalingSettings.Applies) return;

                var playerCount = RunInfo.PlayerCount(combatState?.RunState);
                var stats = ScalingSettings.Current.For(playerCount);

                // Debuffs enemies put on players.
                if (target.Side == CombatSide.Player && giver != null && giver.Side == CombatSide.Enemy
                    && canonicalPower.Type == PowerType.Debuff)
                {
                    var mult = ScalingSettings.ToMultiplier(stats.Debuffs);
                    if (mult != 1m) __result = Math.Max(1m, RoundAmount(__result * mult));
                    return;
                }

                // Solo only: buffs enemies give themselves (the game has no co-op scaling to adjust at 1 player).
                if (playerCount <= 1 && target.Side == CombatSide.Enemy && (giver == null || giver.Side == CombatSide.Enemy)
                    && canonicalPower.Type == PowerType.Buff)
                {
                    var mult = ScalingSettings.ToMultiplier(stats.Buffs);
                    if (mult != 1m) __result = Math.Max(1m, RoundAmount(__result * mult));
                }
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Debuff scaling patch failed: {e}");
            }
        }
    }

}
