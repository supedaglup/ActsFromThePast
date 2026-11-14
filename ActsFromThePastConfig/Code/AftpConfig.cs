using BaseLib.Config;
using Godot;

namespace ActsFromThePastConfig;

public enum ShopMusicChoice
{
    MatchAct,
    Act1,
    Act2,
    Act3,
}

/// <summary>
/// The in-game settings page (Settings > Mod Configuration > "Acts from the Past: Config Sub-Mod").
/// BaseLib builds the UI from these properties; the text for every row comes from <see cref="ModLocalization"/>.
///
/// Slider values are the TOTAL multiplier compared to a single-player enemy. The defaults are the base game's
/// own Act 1 co-op values, so with everything at default the game plays exactly like vanilla.
/// </summary>
[ConfigHoverTipsByDefault]
public class AftpConfig : SimpleModConfig
{
    // ───────────────────────── General ─────────────────────────

    [ConfigSection("GeneralSection")]
    public static bool OnlyInLegacyActs { get; set; } = true;

    public static bool ScaleUpInLaterActs { get; set; } = true;

    public static bool ShopMusicFix { get; set; } = true;

    [ConfigVisibleIf(nameof(ShopMusicFix))]
    public static ShopMusicChoice ShopMusicSource { get; set; } = ShopMusicChoice.MatchAct;

    // ───────────────────────── 2 players ─────────────────────────
    // Base game (Act 1): HP x2.2, Block x2.0, Buffs x2.2, Damage x1.0, Debuffs x1.0

    [ConfigSection("TwoPlayersSection")]
    [ConfigSlider(0.5, 6.0, 0.05, Format = "x{0:0.00}")]
    public static double TwoPlayersHp { get; set; } = 2.2;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double TwoPlayersDamage { get; set; } = 1.0;

    [ConfigSlider(0.5, 6.0, 0.05, Format = "x{0:0.00}")]
    public static double TwoPlayersBlock { get; set; } = 2.0;

    [ConfigSlider(0.5, 6.0, 0.05, Format = "x{0:0.00}")]
    public static double TwoPlayersBuffs { get; set; } = 2.2;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double TwoPlayersDebuffs { get; set; } = 1.0;

    // ───────────────────────── 3 players ─────────────────────────
    // Base game (Act 1): HP x3.3, Block x3.3, Buffs x3.3

    [ConfigSection("ThreePlayersSection")]
    [ConfigSlider(0.5, 8.0, 0.05, Format = "x{0:0.00}")]
    public static double ThreePlayersHp { get; set; } = 3.3;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double ThreePlayersDamage { get; set; } = 1.0;

    [ConfigSlider(0.5, 8.0, 0.05, Format = "x{0:0.00}")]
    public static double ThreePlayersBlock { get; set; } = 3.3;

    [ConfigSlider(0.5, 8.0, 0.05, Format = "x{0:0.00}")]
    public static double ThreePlayersBuffs { get; set; } = 3.3;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double ThreePlayersDebuffs { get; set; } = 1.0;

    // ───────────────────────── 4 players ─────────────────────────
    // Base game (Act 1): HP x4.4, Block x4.4, Buffs x4.4

    [ConfigSection("FourPlayersSection")]
    [ConfigSlider(0.5, 10.0, 0.05, Format = "x{0:0.00}")]
    public static double FourPlayersHp { get; set; } = 4.4;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double FourPlayersDamage { get; set; } = 1.0;

    [ConfigSlider(0.5, 10.0, 0.05, Format = "x{0:0.00}")]
    public static double FourPlayersBlock { get; set; } = 4.4;

    [ConfigSlider(0.5, 10.0, 0.05, Format = "x{0:0.00}")]
    public static double FourPlayersBuffs { get; set; } = 4.4;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double FourPlayersDebuffs { get; set; } = 1.0;

    // ───────────────────────── Solo ─────────────────────────
    // The base game applies no scaling at all in single player; these let you tune solo runs too.

    [ConfigSection("SoloSection")]
    [ConfigSlider(0.25, 4.0, 0.05, Format = "x{0:0.00}")]
    public static double SoloHp { get; set; } = 1.0;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double SoloDamage { get; set; } = 1.0;

    [ConfigSlider(0.25, 4.0, 0.05, Format = "x{0:0.00}")]
    public static double SoloBlock { get; set; } = 1.0;

    [ConfigSlider(0.25, 4.0, 0.05, Format = "x{0:0.00}")]
    public static double SoloBuffs { get; set; } = 1.0;

    [ConfigSlider(0.25, 3.0, 0.05, Format = "x{0:0.00}")]
    public static double SoloDebuffs { get; set; } = 1.0;

    // ───────────────────────── plumbing ─────────────────────────

    private bool _hooked;

    public override void SetupConfigUI(Control optionContainer)
    {
        // Make sure our English strings are in the localization table before BaseLib looks them up.
        ModLocalization.EnsureInjected();

        base.SetupConfigUI(optionContainer);

        if (!_hooked)
        {
            // In co-op the host's settings are what count; re-send them if the host tweaks a slider mid-run.
            ConfigChanged += (_, _) => Scaling.HostSync.OnLocalConfigChanged();
            _hooked = true;
        }
    }
}
