using AdminMenu.Features;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// The mod's own frame, borrowed from the game.
///
/// <c>RunManager</c> exists for the whole run and ticks on every client, which makes its
/// <c>Update</c> the cheapest place to hang the mod's per-frame work without adding a
/// component of its own. What that work is belongs to <see cref="AdminTick"/>; this is only
/// where it is hooked on.
/// </summary>
[HarmonyPatch(typeof(RunManager))]
internal static class MenuHotkeyPatches
{
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void Update_Postfix()
    {
        AdminTick.Run();
    }
}
