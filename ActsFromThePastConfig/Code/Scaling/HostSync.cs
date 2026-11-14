using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePastConfig.Scaling;

/// <summary>
/// In co-op every machine simulates the fight, so everyone must use the same multipliers or the game desyncs.
/// The host's settings win: the host sends them when a co-op run starts or is loaded (and again whenever the host
/// changes a slider during the run), and the other players use those instead of their own sliders.
/// Single player and the host always use their own sliders.
/// </summary>
public static class HostSync
{
    private static void SendIfHost(INetGameService? netService)
    {
        if (netService == null || netService.Type != NetGameType.Host) return;

        // The host always plays by its own config, even if it joined someone else's game earlier this session.
        ScalingSettings.FromHost = null;

        try
        {
            CustomMessageWrapper.Send(SettingsMessage.FromValues(ScalingSettings.Local), netService);
            ModMain.Logger.Info("Sent scaling settings to the other players.");
        }
        catch (Exception e)
        {
            ModMain.Logger.Error($"Could not send scaling settings to other players: {e}");
        }
    }

    /// <summary>Called by the config UI whenever a value changes.</summary>
    public static void OnLocalConfigChanged()
    {
        try
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.IsInProgress || manager.IsSingleplayerOrFakeMultiplayer) return;
            SendIfHost(manager.NetService);
        }
        catch (Exception e)
        {
            ModMain.Logger.Warn($"Could not re-send scaling settings: {e.Message}");
        }
    }

    public sealed class SettingsMessage : ICustomMessage
    {
        // Each stat set is 5 ints: Hp, Damage, Block, Buffs, Debuffs (hundredths).
        public int[] Numbers = new int[20];
        public bool OnlyInLegacyActs;
        public bool ScaleUpInLaterActs;

        public bool ShouldBroadcast => true;

        public static SettingsMessage FromValues(ScalingSettings.Values v)
        {
            var msg = new SettingsMessage
            {
                OnlyInLegacyActs = v.OnlyInLegacyActs,
                ScaleUpInLaterActs = v.ScaleUpInLaterActs,
            };
            Write(msg.Numbers, 0, v.Solo);
            Write(msg.Numbers, 5, v.Two);
            Write(msg.Numbers, 10, v.Three);
            Write(msg.Numbers, 15, v.Four);
            return msg;
        }

        public ScalingSettings.Values ToValues() => new(
            OnlyInLegacyActs,
            ScaleUpInLaterActs,
            Read(Numbers, 0),
            Read(Numbers, 5),
            Read(Numbers, 10),
            Read(Numbers, 15));

        private static void Write(int[] target, int offset, ScalingSettings.StatSet s)
        {
            target[offset + 0] = s.Hp;
            target[offset + 1] = s.Damage;
            target[offset + 2] = s.Block;
            target[offset + 3] = s.Buffs;
            target[offset + 4] = s.Debuffs;
        }

        private static ScalingSettings.StatSet Read(int[] source, int offset) =>
            new(source[offset + 0], source[offset + 1], source[offset + 2], source[offset + 3], source[offset + 4]);

        public void Serialize(PacketWriter writer)
        {
            writer.WriteInt(OnlyInLegacyActs ? 1 : 0, 8);
            writer.WriteInt(ScaleUpInLaterActs ? 1 : 0, 8);
            for (var i = 0; i < Numbers.Length; i++)
                writer.WriteInt(Numbers[i], 32);
        }

        public void Deserialize(PacketReader reader)
        {
            OnlyInLegacyActs = reader.ReadInt(8) != 0;
            ScaleUpInLaterActs = reader.ReadInt(8) != 0;
            for (var i = 0; i < Numbers.Length; i++)
                Numbers[i] = reader.ReadInt(32);
        }

        public void HandleMessage(ulong senderId)
        {
            try
            {
                var manager = RunManager.Instance;
                // The host ignores echoes of its own message.
                if (manager?.NetService != null && manager.NetService.Type == NetGameType.Host) return;

                ScalingSettings.FromHost = ToValues();
                ModMain.Logger.Info("Using the host's Acts from the Past scaling settings for this run.");
            }
            catch (Exception e)
            {
                ModMain.Logger.Error($"Failed to apply host scaling settings: {e}");
            }
        }
    }

    /// <summary>Single-player runs always use the local sliders; forget anything received from an earlier host.</summary>
    [HarmonyPatch]
    public static class ClearOnSinglePlayer
    {
        public static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            var a = AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer));
            var b = AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpSavedSingleplayer));
            if (a != null) yield return a;
            if (b != null) yield return b;
        }

        public static void Prefix() => ScalingSettings.FromHost = null;
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiplayer))]
    public static class SendOnNewRun
    {
        public static void Postfix(StartRunLobby lobby) => SendIfHost(lobby.NetService);
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpSavedMultiplayer))]
    public static class SendOnLoadedRun
    {
        public static void Postfix(LoadRunLobby lobby, ref Task __result) => __result = SendAfter(__result, lobby.NetService);

        private static async Task SendAfter(Task setup, INetGameService netService)
        {
            await setup;
            SendIfHost(netService);
        }
    }
}
