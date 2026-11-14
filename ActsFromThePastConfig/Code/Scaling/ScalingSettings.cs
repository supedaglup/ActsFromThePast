namespace ActsFromThePastConfig.Scaling;

/// <summary>
/// The multipliers in effect for the current run.
///
/// Values are kept as whole hundredths (x2.20 = 220) so that every machine in a co-op run does the exact same
/// arithmetic: the host sends these numbers to the other players (see <see cref="HostSync"/>) and everyone,
/// including the host, reads them from here rather than from the raw slider doubles.
/// </summary>
public static class ScalingSettings
{
    public readonly record struct StatSet(int Hp, int Damage, int Block, int Buffs, int Debuffs)
    {
        public static StatSet Neutral => new(100, 100, 100, 100, 100);
    }

    public readonly record struct Values(
        bool OnlyInLegacyActs,
        bool ScaleUpInLaterActs,
        StatSet Solo,
        StatSet Two,
        StatSet Three,
        StatSet Four)
    {
        public StatSet For(int playerCount) => playerCount switch
        {
            <= 1 => Solo,
            2 => Two,
            3 => Three,
            _ => Four,
        };
    }

    /// <summary>Set by <see cref="HostSync"/> on clients; null means "use my own sliders".</summary>
    internal static Values? FromHost { get; set; }

    public static Values Current => FromHost ?? Local;

    public static Values Local => new(
        AftpConfig.OnlyInLegacyActs,
        AftpConfig.ScaleUpInLaterActs,
        new StatSet(H(AftpConfig.SoloHp), H(AftpConfig.SoloDamage), H(AftpConfig.SoloBlock), H(AftpConfig.SoloBuffs), H(AftpConfig.SoloDebuffs)),
        new StatSet(H(AftpConfig.TwoPlayersHp), H(AftpConfig.TwoPlayersDamage), H(AftpConfig.TwoPlayersBlock), H(AftpConfig.TwoPlayersBuffs), H(AftpConfig.TwoPlayersDebuffs)),
        new StatSet(H(AftpConfig.ThreePlayersHp), H(AftpConfig.ThreePlayersDamage), H(AftpConfig.ThreePlayersBlock), H(AftpConfig.ThreePlayersBuffs), H(AftpConfig.ThreePlayersDebuffs)),
        new StatSet(H(AftpConfig.FourPlayersHp), H(AftpConfig.FourPlayersDamage), H(AftpConfig.FourPlayersBlock), H(AftpConfig.FourPlayersBuffs), H(AftpConfig.FourPlayersDebuffs)));

    private static int H(double x) => (int)Math.Round(x * 100.0, MidpointRounding.AwayFromZero);

    public static decimal ToMultiplier(int hundredths) => hundredths / 100m;

    /// <summary>True if, right now, this mod should change enemy numbers at all.</summary>
    public static bool Applies => !Current.OnlyInLegacyActs || RunInfo.InLegacyAct;
}
