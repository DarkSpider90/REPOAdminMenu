using AdminMenu.Actions;
using AdminMenu.Features;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// Lets the menu choose the next level.
///
/// <c>RunManager.SetRunLevel</c> picks at random from the level list and there is no other way
/// in, so this is the one patch the level controls need. When nothing is parked the original
/// runs untouched and the game behaves exactly as it always does; the patch also clears
/// <c>previousRunLevel</c>, because <c>SetRunLevel</c> refuses to pick the level the run was
/// just on and the menu is allowed to.
/// </summary>
[HarmonyPatch(typeof(RunManager))]
internal static class RunManagerPatches
{
    [HarmonyPatch("SetRunLevel")]
    [HarmonyPrefix]
    private static bool SetRunLevel_Prefix(RunManager __instance, ref Level ___previousRunLevel)
    {
        Level chosen = LevelActions.ConsumePendingLevel();
        if (chosen == null)
        {
            return true;
        }

        // Otherwise the game would reject the choice as a repeat of the level just played.
        ___previousRunLevel = null;
        __instance.levelCurrent = chosen;

        Plugin.LogVerbose($"Level forced to \"{chosen.name}\".");
        return false;
    }

    /// <summary>
    /// Leaving for the main menu ends the run, which is where everything the menu was holding
    /// for it is dropped.
    /// </summary>
    [HarmonyPatch("ChangeLevel")]
    [HarmonyPostfix]
    private static void ChangeLevel_Postfix(RunManager __instance)
    {
        if (__instance.levelCurrent != null && __instance.levelCurrent == __instance.levelMainMenu)
        {
            AdminSession.Reset();
        }
    }
}
