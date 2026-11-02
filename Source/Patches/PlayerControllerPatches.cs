using AdminMenu.Features;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// Infinite stamina, and the frame-by-frame part of free flight.
///
/// <c>PlayerController</c> exists once per machine and belongs to the player sitting at it, so
/// both of these are about whoever is playing here. That is why neither is applied to another
/// player directly: the host grants the ability and this runs on their machine.
/// </summary>
[HarmonyPatch(typeof(PlayerController))]
internal static class PlayerControllerPatches
{
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void Update_Postfix(PlayerController __instance)
    {
        if (AdminFlags.LocalInfiniteStamina)
        {
            __instance.EnergyCurrent = __instance.EnergyStart;
        }

        FreeFlight.Drive();
    }
}
