using AdminMenu.Features;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// Infinite ammo: batteries stay full.
///
/// In R.E.P.O. a gun's ammo is its battery, so holding the charge at full is the whole
/// feature. The bar count is written alongside the charge, otherwise the item keeps drawing
/// the number of bars it had when it was nearly empty.
/// </summary>
[HarmonyPatch(typeof(ItemBattery))]
internal static class ItemBatteryPatches
{
    private const float FullCharge = 100f;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void Update_Postfix(ItemBattery __instance, ref int ___batteryLifeInt)
    {
        if (!AdminFlags.InfiniteAmmo)
        {
            return;
        }

        __instance.batteryLife = FullCharge;

        // How many bars a full battery has is a property of the item, not a constant: a drone
        // has a different count from a gun. Writing a fixed number here is what made every
        // battery in the lobby drop to whatever fraction of its own scale that number was.
        ___batteryLifeInt = __instance.batteryBars;
    }
}
