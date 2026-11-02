using System;
using System.Collections.Generic;
using System.Reflection;
using AdminMenu.Configuration;
using AdminMenu.Localization;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AdminMenu;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
// REPOLib is optional: without it the menu still works, on vanilla content only. MenuLib is
// not - it is the menu itself, so a missing MenuLib should say so plainly at startup rather
// than throw the first time the key is pressed.
[BepInDependency("REPOLib", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("nickklmao.menulib", BepInDependency.DependencyFlags.HardDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; }

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        AdminMenuSettings.Bind(Config);

        LanguageFiles.LoadAll();
        if (AdminMenuSettings.WriteLanguageTemplate.Value)
        {
            LanguageFiles.WriteTemplate();
        }

        ApplyPatches();

        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded for R.E.P.O. v0.5.0.");
    }

    private void OnDestroy()
    {
        LocaleWatcher.Unsubscribe();
        _harmony?.UnpatchSelf();
    }

    /// <summary>
    /// Logs only when the user turned diagnostics on, so a normal session stays quiet.
    /// </summary>
    internal static void LogVerbose(string message)
    {
        if (AdminMenuSettings.VerboseLogging != null && AdminMenuSettings.VerboseLogging.Value)
        {
            Log.LogInfo(message);
        }
    }

    /// <summary>
    /// Patches one class at a time, so a single patch a game update invalidates cannot take
    /// the whole mod down with it the way <c>Harmony.PatchAll</c> does.
    /// </summary>
    private void ApplyPatches()
    {
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                List<MethodInfo> patched = _harmony.CreateClassProcessor(type).Patch();
                if (patched != null && patched.Count > 0)
                {
                    continue;
                }

                // Harmony passes over a class that carries no [HarmonyPatch] attribute of its
                // own, however its methods are annotated, and says nothing about it. Catch
                // that here rather than in a bug report.
                if (DeclaresPatchMethods(type))
                {
                    Log.LogError(
                        $"{type.FullName} declares Harmony patch methods but carries no [HarmonyPatch] " +
                        "attribute on the class itself, so none of them were applied.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply patches from {type.FullName}; continuing. {ex}");
            }
        }
    }

    private static bool DeclaresPatchMethods(Type type)
    {
        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
        {
            if (method.GetCustomAttribute<HarmonyPrefix>() != null ||
                method.GetCustomAttribute<HarmonyPostfix>() != null ||
                method.GetCustomAttribute<HarmonyTranspiler>() != null ||
                method.GetCustomAttribute<HarmonyFinalizer>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
