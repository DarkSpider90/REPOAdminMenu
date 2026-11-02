using AdminMenu.Localization;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// The first moment the game's language is known. The mod loads before the game has read
/// the player's saved language, so the language template - which is written in that
/// language - waits for <c>LocalizationManager.Start</c>, where the game loads it.
/// </summary>
[HarmonyPatch(typeof(LocalizationManager))]
internal static class LocalizationManagerPatches
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void Start_Postfix()
    {
        LanguageFiles.WriteTemplateIfRequested();
    }
}
