using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;

namespace ActsFromThePastConfig;

/// <summary>
/// All text shown in the settings page.
///
/// Normally a mod ships these strings in a .pck file built with Godot. To keep this mod a single .dll that anyone
/// can build with just Visual Studio, the strings are written straight into the game's "settings_ui" localization
/// table at runtime instead (the same trick BaseLib itself uses for in-code localization).
/// </summary>
public static class ModLocalization
{
    private const string Table = "settings_ui";
    private const string Prefix = "ACTSFROMTHEPASTCONFIG-";
    private const string MarkerKey = "ACTSFROMTHEPASTCONFIG.mod_title";

    // (name, title, hover description). Section names and property names must match AftpConfig exactly;
    // they are turned into keys with the game's own StringHelper.Slugify at injection time.
    private static readonly (string Name, string Title, string? Desc)[] Rows =
    {
        // General
        ("GeneralSection", "General", null),
        ("OnlyInLegacyActs", "Only change enemies in Acts from the Past acts",
            "ON: the multipliers below only apply while you are in Exordium, The City or The Beyond (the Slay the Spire 1 acts). Other acts keep the base game's co-op scaling.\n\nOFF: the multipliers apply in every act, including base game acts and acts from other mods.\n\nDefault: ON."),
        ("ScaleUpInLaterActs", "Scale up slightly in later acts (like the base game)",
            "The base game makes co-op enemies a bit tougher in later acts: Act 1 is x1.1 per player, Act 2 is x1.2, and the Act 3 boss is x1.3.\n\nON: the sliders below are your Act 1 values, and later acts go up by the same proportion as the base game.\n\nOFF: the exact slider value is used in every act.\n\nDefault: ON."),
        ("ShopMusicFix", "Play the regular shop music in Acts from the Past shops",
            "Shops inside the Slay the Spire 1 acts normally go quiet because the custom acts have no shop music. With this ON, the regular Slay the Spire 2 merchant music plays there instead.\n\nDefault: ON."),
        ("ShopMusicSource", "Which act's shop music to use",
            "Match act: Exordium uses the Act 1 music, The City uses Act 2, The Beyond uses Act 3.\n\nOr pick one act's shop music for all three."),

        // 2 players
        ("TwoPlayersSection", "2 Players", null),
        ("TwoPlayersHp", "Enemy HP",
            "Enemy max HP compared to a solo run, with 2 players.\n\nBase game value: x2.20 (Act 1). Set to x1.00 for solo-run HP.\n\nAlso adjusts the two Acts from the Past enemies that scale themselves: Awakened One's phase 2 HP and the Transient's attack damage (its HP doesn't matter since it fades away)."),
        ("TwoPlayersDamage", "Enemy attack damage",
            "Multiplies the base damage of every enemy attack, with 2 players.\n\nBase game value: x1.00 (the base game never scales enemy damage in co-op, but every player gets hit)."),
        ("TwoPlayersBlock", "Enemy Block",
            "Block gained by enemies compared to a solo run, with 2 players.\n\nBase game value: x2.00."),
        ("TwoPlayersBuffs", "Enemy buffs (Regen, Metallicize, Plated Armor...)",
            "How strongly the buffs that the base game scales in co-op are scaled, with 2 players. Special cases like Artifact and Plating keep their own base-game formulas but are reduced or increased in proportion.\n\nBase game value: x2.20 (Act 1). Set to x1.00 for solo-run amounts."),
        ("TwoPlayersDebuffs", "Debuffs on players (Weak, Vulnerable, Frail...)",
            "Multiplies the amount of every debuff enemies put on players, with 2 players.\n\nBase game value: x1.00."),

        // 3 players
        ("ThreePlayersSection", "3 Players", null),
        ("ThreePlayersHp", "Enemy HP",
            "Enemy max HP compared to a solo run, with 3 players.\n\nBase game value: x3.30 (Act 1).\n\nAlso adjusts the two Acts from the Past enemies that scale themselves: Awakened One's phase 2 HP and the Transient's attack damage (its HP doesn't matter since it fades away)."),
        ("ThreePlayersDamage", "Enemy attack damage",
            "Multiplies the base damage of every enemy attack, with 3 players.\n\nBase game value: x1.00."),
        ("ThreePlayersBlock", "Enemy Block",
            "Block gained by enemies compared to a solo run, with 3 players.\n\nBase game value: x3.30 (Act 1)."),
        ("ThreePlayersBuffs", "Enemy buffs (Regen, Metallicize, Plated Armor...)",
            "How strongly the buffs that the base game scales in co-op are scaled, with 3 players.\n\nBase game value: x3.30 (Act 1)."),
        ("ThreePlayersDebuffs", "Debuffs on players (Weak, Vulnerable, Frail...)",
            "Multiplies the amount of every debuff enemies put on players, with 3 players.\n\nBase game value: x1.00."),

        // 4 players
        ("FourPlayersSection", "4 Players", null),
        ("FourPlayersHp", "Enemy HP",
            "Enemy max HP compared to a solo run, with 4 players (also used for larger lobbies from other mods).\n\nBase game value: x4.40 (Act 1).\n\nAlso adjusts the two Acts from the Past enemies that scale themselves: Awakened One's phase 2 HP and the Transient's attack damage (its HP doesn't matter since it fades away)."),
        ("FourPlayersDamage", "Enemy attack damage",
            "Multiplies the base damage of every enemy attack, with 4 players.\n\nBase game value: x1.00."),
        ("FourPlayersBlock", "Enemy Block",
            "Block gained by enemies compared to a solo run, with 4 players.\n\nBase game value: x4.40 (Act 1)."),
        ("FourPlayersBuffs", "Enemy buffs (Regen, Metallicize, Plated Armor...)",
            "How strongly the buffs that the base game scales in co-op are scaled, with 4 players.\n\nBase game value: x4.40 (Act 1)."),
        ("FourPlayersDebuffs", "Debuffs on players (Weak, Vulnerable, Frail...)",
            "Multiplies the amount of every debuff enemies put on players, with 4 players.\n\nBase game value: x1.00."),

        // Solo
        ("SoloSection", "Solo (1 Player)", null),
        ("SoloHp", "Enemy HP", "Enemy max HP in single player.\n\nBase game value: x1.00."),
        ("SoloDamage", "Enemy attack damage", "Multiplies the base damage of every enemy attack in single player.\n\nBase game value: x1.00."),
        ("SoloBlock", "Enemy Block", "Block gained by enemies in single player.\n\nBase game value: x1.00."),
        ("SoloBuffs", "Enemy buffs", "Multiplies every buff enemies give themselves in single player.\n\nBase game value: x1.00."),
        ("SoloDebuffs", "Debuffs on players", "Multiplies the amount of every debuff enemies put on you in single player.\n\nBase game value: x1.00."),
    };

    // Dropdown entries: (property name, enum value name, label)
    private static readonly (string Property, string Value, string Label)[] DropdownValues =
    {
        ("ShopMusicSource", nameof(ShopMusicChoice.MatchAct), "Match act"),
        ("ShopMusicSource", nameof(ShopMusicChoice.Act1), "Act 1 music"),
        ("ShopMusicSource", nameof(ShopMusicChoice.Act2), "Act 2 music"),
        ("ShopMusicSource", nameof(ShopMusicChoice.Act3), "Act 3 music"),
    };

    private static readonly FieldInfo? TranslationsField = FindTranslationsField();

    private static FieldInfo? FindTranslationsField()
    {
        var field = AccessTools.Field(typeof(LocTable), "_translations");
        if (field != null) return field;

        // Fallback: any Dictionary<string, string> field on LocTable.
        return typeof(LocTable)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(f => f.FieldType == typeof(Dictionary<string, string>));
    }

    /// <summary>Writes the strings into the settings_ui table if they are not there yet. Safe to call often.</summary>
    public static void EnsureInjected()
    {
        try
        {
            var locManager = LocManager.Instance;
            if (locManager == null) return;
            Inject(locManager.GetTable(Table));
        }
        catch (Exception e)
        {
            ModMain.Logger.Warn($"Could not inject settings text: {e.Message}");
        }
    }

    internal static void Inject(LocTable? table)
    {
        if (table == null || TranslationsField == null) return;
        if (TranslationsField.GetValue(table) is not Dictionary<string, string> dict) return;
        if (dict.ContainsKey(MarkerKey)) return; // already done for this language load

        dict[MarkerKey] = "Acts from the Past: Config Sub-Mod";

        foreach (var (name, title, desc) in Rows)
        {
            var key = Prefix + StringHelper.Slugify(name);
            dict.TryAdd(key + ".title", title);
            if (desc != null) dict.TryAdd(key + ".hover.desc", desc);
        }

        foreach (var (property, value, label) in DropdownValues)
        {
            dict.TryAdd($"{Prefix}{StringHelper.Slugify(property)}.{value}", label);
        }
    }

    /// <summary>
    /// Every time the game hands out the settings_ui table (which happens for each lookup, and again after a
    /// language change rebuilds the tables) make sure our strings are present.
    /// </summary>
    [HarmonyPatch(typeof(LocManager), nameof(LocManager.GetTable), typeof(string))]
    public static class GetTablePatch
    {
        public static void Postfix(object[] __args, LocTable? __result)
        {
            if (__result == null) return;
            if (__args.Length == 0 || __args[0] is not string name || name != Table) return;
            Inject(__result);
        }
    }
}
