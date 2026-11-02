using AdminMenu.Features;
using HarmonyLib;
using UnityEngine;

namespace AdminMenu.Patches;

/// <summary>
/// Immortality.
///
/// Two entry points, because damage reaches a player from two directions. <c>HurtOther</c>
/// runs wherever the damage comes from and then tells the owner, so the host blocking it here
/// covers everything the host simulates: enemies, traps, falling objects, other players.
/// <c>Hurt</c> runs on the player's own machine, so blocking it there covers what that machine
/// works out for itself, a fall for instance.
///
/// The consequence is worth being plain about: a host can make any player immortal against
/// the world, but damage a remote player's own machine decides on is only stopped if that
/// player is running this mod too. The game offers no way around that - it checks
/// <c>photonView.IsMine</c> before it applies damage at all.
/// </summary>
[HarmonyPatch(typeof(PlayerHealth))]
internal static class PlayerHealthPatches
{
    [HarmonyPatch("Hurt")]
    [HarmonyPrefix]
    private static bool Hurt_Prefix(PlayerAvatar ___playerAvatar)
    {
        return !AdminFlags.IsImmortal(___playerAvatar);
    }

    [HarmonyPatch("HurtOther")]
    [HarmonyPrefix]
    private static bool HurtOther_Prefix(PlayerAvatar ___playerAvatar)
    {
        return !AdminFlags.IsImmortal(___playerAvatar);
    }
}
