using System.Reflection;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using ActsFromThePast.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Dread;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Dread Mode support (Slay the Spire 2 v0.113.0+).
//
// In Dread, the game asks each act's boss encounter for a "dominion" (EncounterModel.GetDominion) and applies it to
// every fight in that act except the boss fight itself. Each Slay the Spire 1 boss now returns one of the dominions
// below, themed on that boss and sized like the base game's dominions for the same act:
//   Act 1: Ritual 1 / Weak 2 + Frail 2 / one status card      Act 2: Plating 7 / two curses
//   Act 3: Strength 3 / Weak 3 + Vulnerable 3 + Frail 3
// The game discovers these classes on its own (every AbstractModel subclass in a mod is registered).
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

internal static class DominionHelpers
{
    public static IEnumerable<Creature> PlayerCreatures(AbstractRoom room) =>
        room is CombatRoom combat ? combat.CombatState.Players.Select(p => p.Creature) : Enumerable.Empty<Creature>();

    public static IEnumerable<IHoverTip> Concat(params IEnumerable<IHoverTip>[] tips) => tips.SelectMany(t => t);
}

// ── Exordium ──────────────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>Slime Boss (Goop Spray): each player starts every combat with 2 Slimed shuffled into their draw pile.</summary>
public sealed class AftpSlimeBossDominion : DominionModel
{
    public const int SlimedCount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Slimed>(false);

    protected override async Task AfterPlayersEnteredCombat(IReadOnlyList<Player> players)
    {
        // Same switch as Acts from the Past's own slimes: optionally the classic (no draw) Slimed.
        ClassicSlimedTracker.CreatingClassicSlimed = ActsFromThePastConfig.LegacyEnemiesGiveClassicSlimed;
        try
        {
            foreach (var player in players)
                await CardPileCmd.AddToCombatAndPreview<Slimed>(player.Creature, PileType.Draw, SlimedCount, player, CardPilePosition.Random);
        }
        finally
        {
            ClassicSlimedTracker.CreatingClassicSlimed = false;
        }
    }
}

/// <summary>Hexaghost (Sear): each player starts every combat with 2 Burns shuffled into their draw pile.</summary>
public sealed class AftpHexaghostDominion : DominionModel
{
    public const int BurnCount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Burn>(false);

    protected override async Task AfterPlayersEnteredCombat(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
            await CardPileCmd.AddToCombatAndPreview<Burn>(player.Creature, PileType.Draw, BurnCount, player, CardPilePosition.Random);
    }
}

/// <summary>The Guardian (Sharp Hide): all enemies have 2 Thorns.</summary>
public sealed class AftpGuardianDominion : DominionModel
{
    public const int ThornsAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<ThornsPower>(ThornsAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), creatures, ThornsAmount, null, null, false);
}

// ── The City ──────────────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>The Collector (Mega Debuff): players start every combat with 2 Weak, 2 Vulnerable and 2 Frail.</summary>
public sealed class AftpCollectorDominion : DominionModel
{
    public const int DebuffAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => DominionHelpers.Concat(
        HoverTipFactory.FromPowerWithPowerHoverTips<WeakPower>(DebuffAmount),
        HoverTipFactory.FromPowerWithPowerHoverTips<VulnerablePower>(DebuffAmount),
        HoverTipFactory.FromPowerWithPowerHoverTips<FrailPower>(DebuffAmount));

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room); // keeps the base class's bookkeeping (enemy hook, players entering combat)
        var players = DominionHelpers.PlayerCreatures(room).ToList();
        if (players.Count == 0) return;
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), players, DebuffAmount, null, null, false);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), players, DebuffAmount, null, null, false);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), players, DebuffAmount, null, null, false);
    }
}

/// <summary>The Champ (Anger): all enemies have 2 Strength.</summary>
public sealed class AftpChampDominion : DominionModel
{
    public const int StrengthAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<StrengthPower>(StrengthAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), creatures, StrengthAmount, null, null, false);
}

/// <summary>Bronze Automaton (its Artifact): all enemies have 2 Artifact.</summary>
public sealed class AftpBronzeAutomatonDominion : DominionModel
{
    public const int ArtifactAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<ArtifactPower>(ArtifactAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), creatures, ArtifactAmount, null, null, false);
}

// ── The Beyond ────────────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>Awakened One (Curiosity): all enemies have 2 Curiosity (gain 2 Strength whenever a Power is played).</summary>
public sealed class AftpAwakenedOneDominion : DominionModel
{
    public const int CuriosityAmount = 2;

    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<CuriosityPower>(CuriosityAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures) =>
        PowerCmd.Apply<CuriosityPower>(new ThrowingPlayerChoiceContext(), creatures, CuriosityAmount, null, null, false);
}

/// <summary>
/// Time Eater (Time Warp): the toughest enemy in every fight carries Time Warp. After 12 cards per player, the
/// players' turn ends and every enemy gains 2 Strength. Only one enemy per fight gets it, so the turn never ends twice.
/// </summary>
public sealed class AftpTimeEaterDominion : DominionModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromPowerWithPowerHoverTips<TimeWarpPower>(null);

    protected override async Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        var entering = creatures.Where(c => c.IsAlive).ToList();
        if (entering.Count == 0) return;

        var combat = entering[0].CombatState;
        if (combat != null && combat.Enemies.Any(e => e.IsAlive && e.Powers.OfType<TimeWarpPower>().Any()))
            return; // someone already carries it

        var carrier = entering.OrderByDescending(c => c.MaxHp).First();
        await PowerCmd.Apply<TimeWarpPower>(new ThrowingPlayerChoiceContext(), carrier, 1M, null, null, false);
    }
}

/// <summary>Donu and Deca (Circle of Power + Square of Protection): all enemies have 1 Strength and 4 Plating.</summary>
public sealed class AftpDonuAndDecaDominion : DominionModel
{
    public const int StrengthAmount = 1;
    public const int PlatingAmount = 4;

    public override IEnumerable<IHoverTip> ExtraHoverTips => DominionHelpers.Concat(
        HoverTipFactory.FromPowerWithPowerHoverTips<StrengthPower>(StrengthAmount),
        HoverTipFactory.FromPowerWithPowerHoverTips<PlatingPower>(PlatingAmount));

    protected override async Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        var list = creatures.ToList();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), list, StrengthAmount, null, null, false);
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), list, PlatingAmount, null, null, false);
    }
}

// ── Dread boss strength ───────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// In Dread the base game makes every boss tougher, boss by boss (roughly +6% to +12% HP plus a stronger signature
/// move). The Slay the Spire 1 bosses get a matching flat boost: +10% max HP in Dread.
/// </summary>
public static class LegacyDread
{
    public const decimal BossHpMultiplier = 1.10M;

    public static int BossHp(int hp)
    {
        try
        {
            if (!AscensionHelper.IsDread()) return hp;
        }
        catch
        {
            return hp; // no run (e.g. compendium) - use normal values
        }
        return (int)Math.Ceiling(hp * BossHpMultiplier);
    }
}

// ── Text ──────────────────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The dominion text shown on the boss icon. The game reads it from the "ascension" table under
/// DREAD.{model id without "_DOMINION"}.description, so it is added there in code, keyed from each model's real id.
/// </summary>
[HarmonyPatch(typeof(LocManager), nameof(LocManager.GetTable), typeof(string))]
public static class DominionLocalization
{
    private static readonly (Type Type, string Text)[] Texts =
    {
        (typeof(AftpSlimeBossDominion), "At the start of each combat, shuffle [gold]2[/gold] [gold]Slimed[/gold] into your Draw Pile."),
        (typeof(AftpHexaghostDominion), "At the start of each combat, shuffle [gold]2[/gold] [gold]Burns[/gold] into your Draw Pile."),
        (typeof(AftpGuardianDominion), "Enemies have [gold]2[/gold] [gold]Thorns[/gold]."),
        (typeof(AftpCollectorDominion), "At the start of each combat, gain [gold]2[/gold] [gold]Weak[/gold], [gold]2[/gold] [gold]Vulnerable[/gold] and [gold]2[/gold] [gold]Frail[/gold]."),
        (typeof(AftpChampDominion), "Enemies have [gold]2[/gold] [gold]Strength[/gold]."),
        (typeof(AftpBronzeAutomatonDominion), "Enemies have [gold]2[/gold] [gold]Artifact[/gold]."),
        (typeof(AftpAwakenedOneDominion), "Enemies have [gold]2[/gold] [gold]Curiosity[/gold]."),
        (typeof(AftpTimeEaterDominion), "The toughest enemy in each combat has [gold]Time Warp[/gold]."),
        (typeof(AftpDonuAndDecaDominion), "Enemies have [gold]1[/gold] [gold]Strength[/gold] and [gold]4[/gold] [gold]Plating[/gold]."),
    };

    private static readonly FieldInfo? TranslationsField = AccessTools.Field(typeof(LocTable), "_translations");
    private static Dictionary<string, string>? _keys;

    private static Dictionary<string, string> Keys()
    {
        if (_keys != null) return _keys;
        var keys = new Dictionary<string, string>();
        foreach (var (type, text) in Texts)
        {
            var entry = ModelDb.GetEntry(type); // e.g. AFTP_SLIME_BOSS_DOMINION (same rule the game uses)
            const string suffix = "_DOMINION";
            if (entry.EndsWith(suffix)) entry = entry[..^suffix.Length];
            keys[$"DREAD.{entry}.description"] = text;
        }
        return _keys = keys;
    }

    public static void Postfix(string name, LocTable? __result)
    {
        if (name != "ascension" || __result == null) return;
        try
        {
            if (TranslationsField?.GetValue(__result) is not Dictionary<string, string> dict) return;
            foreach (var (key, text) in Keys())
                dict.TryAdd(key, text); // a translation in a .json file, if one is ever added, wins
        }
        catch (Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn($"[ActsFromThePast] Could not add Dread dominion text: {e.Message}");
        }
    }
}
