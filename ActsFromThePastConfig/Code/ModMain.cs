using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace ActsFromThePastConfig;

/// <summary>
/// Entry point. The game finds this through the [ModInitializer] attribute and calls Initialize() once.
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class ModMain
{
    public const string ModId = "ActsFromThePastConfig";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    private static Harmony? _harmony;

    public static void Initialize()
    {
        Logger.Info("Initializing Acts from the Past: Config Sub-Mod");

        _harmony = new Harmony("actsfromthepastconfig.main");

        // Patch class-by-class so that a single patch failing (e.g. after a game update renames a method)
        // does not take the whole mod down with it.
        Type[] types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            // A game update removed a type one of our files mentions; keep whatever still loads.
            Logger.Error($"Some patch classes could not be loaded: {string.Join("; ", e.LoaderExceptions.Select(x => x?.Message))}");
            types = e.Types.Where(t => t != null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type.GetCustomAttribute<HarmonyPatch>() == null) continue;
            if (type.IsNested && type.DeclaringType?.GetCustomAttribute<HarmonyPatch>() != null) continue;

            try
            {
                var processor = _harmony.CreateClassProcessor(type);
                processor.Patch();
                Logger.Info($"Patched: {type.FullName}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to apply patch class {type.FullName}: {e}");
            }
        }

        // Patches that need to adapt to the game's method shape at runtime.
        Scaling.ScalingPatches.ApplyManualPatches(_harmony);

        ModConfigRegistry.Register(ModId, new AftpConfig());

        Logger.Info("Initialized.");
    }
}
