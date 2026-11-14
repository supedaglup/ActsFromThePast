using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace ActsFromThePastConfig.Music;

/// <summary>
/// Shop music for the Slay the Spire 1 acts.
///
/// Why shops are silent: the base game's music is one FMOD event per act with a global "Progress" parameter
/// (2 = Merchant, 9 = Merchant while the map is open). The Acts from the Past acts have no FMOD music of their own,
/// so when it hands control back to the game inside a shop there is simply nothing to switch to.
///
/// The fix: while standing in a shop inside a legacy act, load one of the base game's act music banks, start its
/// music event directly through FMOD (via BaseLib's FmodAudio helper) and set Progress to the merchant section.
/// It is stopped (with FMOD's own fade-out) as soon as the room changes.
/// </summary>
public static class ShopMusic
{
    private const string BankFolder = "res://banks/desktop/";

    private static GodotObject? _musicInstance;
    private static readonly HashSet<string> LoadedBanks = new();
    private static string? _playingEvent;

    public static bool IsPlaying => _musicInstance != null && _playingEvent != null;

    /// <summary>Decide whether shop music should be playing right now, and start/stop accordingly.</summary>
    public static void Update()
    {
        try
        {
            var wanted = AftpConfig.ShopMusicFix && RunInfo.InLegacyAct && RunInfo.InShopRoom;
            if (wanted) Start();
            else Stop();
        }
        catch (Exception e)
        {
            ModMain.Logger.Error($"Shop music update failed: {e}");
        }
    }

    private static int ChooseActNumber()
    {
        var legacyAct = RunInfo.LegacyActNumber;
        return AftpConfig.ShopMusicSource switch
        {
            ShopMusicChoice.Act1 => 1,
            ShopMusicChoice.Act2 => 2,
            ShopMusicChoice.Act3 => 3,
            _ => legacyAct is >= 1 and <= 3 ? legacyAct : 1,
        };
    }

    private static void Start()
    {
        if (IsPlaying) return;
        var act = ChooseActNumber();
        _unloadGeneration++; // cancel any pending bank unload
        LoadBanksForAct(act);

        var eventPath = FindMusicEvent(act);
        if (eventPath == null)
        {
            ModMain.Logger.Warn($"No base game music event found for act {act}; shop music fix skipped.");
            return;
        }

        var instance = FmodAudio.CreateEventInstance(eventPath);
        if (instance == null)
        {
            ModMain.Logger.Warn($"Could not create music instance for {eventPath}.");
            return;
        }

        SetMerchantProgress(mapOpen: false);
        instance.Call("start");
        // Set it again after starting: some events only read the parameter once they are running.
        SetMerchantProgress(mapOpen: false);

        _musicInstance = instance;
        _playingEvent = eventPath;
        ModMain.Logger.Info($"Playing base game shop music: {eventPath}");
    }

    public static void Stop(bool immediate = false)
    {
        if (_musicInstance == null)
        {
            if (LoadedBanks.Count > 0) ScheduleBankUnload(immediate ? 0f : UnloadDelaySeconds);
            return;
        }

        try
        {
            if (GodotObject.IsInstanceValid(_musicInstance))
            {
                _musicInstance.Call("stop", immediate ? 1 : 0); // 0 = allow FMOD fade-out, 1 = immediate
                _musicInstance.Call("release");
            }
        }
        catch (Exception e)
        {
            ModMain.Logger.Warn($"Stopping shop music failed: {e.Message}");
        }
        finally
        {
            _musicInstance = null;
            _playingEvent = null;
        }

        // Give the fade-out time to finish, then hand the banks back so the base game can load them itself later
        // (for example when the run moves on to a regular Slay the Spire 2 act).
        ScheduleBankUnload(immediate ? 0f : UnloadDelaySeconds);
    }

    private const float UnloadDelaySeconds = 3f;
    private static int _unloadGeneration;

    private static void ScheduleBankUnload(float delaySeconds)
    {
        var generation = ++_unloadGeneration;
        if (delaySeconds <= 0f)
        {
            UnloadBanks();
            return;
        }

        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree == null)
            {
                UnloadBanks();
                return;
            }

            tree.CreateTimer(delaySeconds).Connect("timeout", Callable.From(() =>
            {
                // A newer Start()/Stop() happened in the meantime: that one owns the banks now.
                if (generation != _unloadGeneration || IsPlaying) return;
                UnloadBanks();
            }));
        }
        catch
        {
            UnloadBanks();
        }
    }

    private static void UnloadBanks()
    {
        foreach (var bank in LoadedBanks)
        {
            try { FmodAudio.UnloadBank(bank); }
            catch { /* ignore */ }
        }
        LoadedBanks.Clear();
    }

    public static void SetMerchantProgress(bool mapOpen)
    {
        try
        {
            // 2 = Merchant, 9 = MerchantEnd (the softer version used while the map is open)
            FmodAudio.SetGlobalParameter("Progress", mapOpen ? 9f : 2f);
        }
        catch (Exception e)
        {
            ModMain.Logger.Warn($"Could not set music Progress parameter: {e.Message}");
        }
    }

    private static void LoadBanksForAct(int act)
    {
        foreach (var variant in new[] { "a1", "a2" })
        {
            var path = $"{BankFolder}act{act}_{variant}.bank";
            if (LoadedBanks.Contains(path)) continue;
            if (!Godot.FileAccess.FileExists(path)) continue;
            if (FmodAudio.LoadBank(path)) LoadedBanks.Add(path);
        }
    }

    private static string? FindMusicEvent(int act)
    {
        var candidates = new[]
        {
            $"event:/music/act{act}_a1_v1",
            $"event:/music/act{act}_a1_v2",
            $"event:/music/act{act}_a2_v1",
            $"event:/music/act{act}_a2_v2",
            $"event:/music/act{act}_a1",
            $"event:/music/act{act}_a2",
            $"event:/music/act{act}",
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (FmodAudio.EventExists(candidate)) return candidate;
            }
            catch
            {
                // keep looking
            }
        }
        return null;
    }
}

/// <summary>Hooks into the game's music controller. These run AFTER Acts from the Past's own music patches.</summary>
public static class ShopMusicPatches
{
    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateTrack), new Type[0])]
    public static class UpdateTrackPatch
    {
        public static void Postfix() => ShopMusic.Update();
    }

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))]
    public static class UpdateMusicPatch
    {
        public static void Postfix() => ShopMusic.Update();
    }

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.StopMusic))]
    public static class StopMusicPatch
    {
        public static void Postfix() => ShopMusic.Stop();
    }

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.ToggleMerchantTrack))]
    public static class ToggleMerchantTrackPatch
    {
        public static void Postfix()
        {
            if (!ShopMusic.IsPlaying) return;
            bool mapVisible;
            try { mapVisible = NMapScreen.Instance?.IsVisible() ?? false; }
            catch { mapVisible = false; }
            ShopMusic.SetMerchantProgress(mapVisible);
        }
    }

    /// <summary>
    /// Acts from the Past also tries to start base game music in shops through the game's audio proxy. When our fix
    /// is on we turn that attempt off so the two never play on top of each other. If the mod's internals change and
    /// this method no longer exists, this patch is simply skipped.
    /// </summary>
    [HarmonyPatch]
    public static class SuppressAftpShopMusicAttempt
    {
        public static bool Prepare()
        {
            var method = FindTarget();
            if (method == null)
                ModMain.Logger.Info("Acts from the Past's StartBaseGameMusic not found; nothing to suppress.");
            return method != null;
        }

        public static System.Reflection.MethodBase? TargetMethod() => FindTarget();

        private static System.Reflection.MethodBase? FindTarget()
        {
            var type = AccessTools.TypeByName("ActsFromThePast.Patches.Audio.MusicPatches+LegacyActMusicPatches")
                       ?? AccessTools.TypeByName("ActsFromThePast.Patches.Audio.MusicPatches");
            return type == null ? null : AccessTools.Method(type, "StartBaseGameMusic");
        }

        public static bool Prefix() => !(AftpConfig.ShopMusicFix && RunInfo.InLegacyAct && RunInfo.InShopRoom);
    }
}
