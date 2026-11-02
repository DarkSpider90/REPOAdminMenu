using AdminMenu.Features;
using HarmonyLib;
using Photon.Pun;

namespace AdminMenu.Patches;

/// <summary>
/// Free shop items.
///
/// The game works a shop item's price out once, on the host, in <c>ItemAttributes.GetValue</c>
/// and then sends it to everyone. Zeroing it right after that one call is enough: the price
/// tag, the running total and the deduction at the till all read the same field. The new price
/// is sent on again, because the original send happened before this ran.
/// </summary>
[HarmonyPatch(typeof(ItemAttributes))]
internal static class ItemAttributesPatches
{
    [HarmonyPatch("GetValue")]
    [HarmonyPostfix]
    private static void GetValue_Postfix(ItemAttributes __instance, ref int ___value)
    {
        if (!AdminFlags.FreeShopItems || ___value == 0)
        {
            return;
        }

        ___value = 0;

        if (!SemiFunc.IsMultiplayer())
        {
            return;
        }

        PhotonView photonView = __instance.GetComponent<PhotonView>();
        if (photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("GetValueRPC", RpcTarget.Others, 0);
        }
    }
}
