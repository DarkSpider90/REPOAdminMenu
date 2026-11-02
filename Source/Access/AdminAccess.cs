using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;

namespace AdminMenu.Access;

/// <summary>
/// Who may use the menu.
///
/// Today: the host, and nobody else. That is not a policy this mod enforces on its own - the
/// game's own RPCs for upgrades, aggro and run stats check the master client and drop
/// anything else, so a client with a menu would have buttons that do nothing.
///
/// The grant list is the shape of the next step rather than a feature that works yet. For a
/// granted player to actually change anything, the host has to carry out their request: the
/// client sends what it wants, the host checks this list and performs the action with its own
/// authority. Until that relay exists, <see cref="GrantsAreEffective"/> stays false and the
/// list only records intent, so the relay can be added without moving the permission checks
/// that are already in place.
/// </summary>
internal static class AdminAccess
{
    /// <summary>
    /// Turns the grant list into real access. Needs the host-side relay first; see the class
    /// summary.
    /// </summary>
    private static readonly bool GrantsAreEffective = false;

    private static readonly HashSet<string> GrantedSteamIds = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Raised when the grant list changes, so an open menu can redraw.</summary>
    internal static event Action Changed;

    internal static bool IsHost => SemiFunc.IsMasterClientOrSingleplayer();

    internal static IReadOnlyCollection<string> Granted => GrantedSteamIds;

    /// <summary>True when this client may open the menu at all.</summary>
    internal static bool CanOpenMenu => IsHost || (GrantsAreEffective && IsGranted(LocalSteamId()));

    /// <summary>
    /// True when this client may actually change the run. Separate from
    /// <see cref="CanOpenMenu"/> on purpose: every action asks this, so a client that is one
    /// day allowed to open the menu still cannot act until the relay is there.
    /// </summary>
    internal static bool CanAct => PunBridge.CanAct;

    /// <summary>Why the menu is refused, in the player's language.</summary>
    internal static string RefusalReason()
    {
        return Strings.Get(StringKeys.MenuHostOnly);
    }

    internal static bool IsGranted(string steamId)
    {
        return !string.IsNullOrEmpty(steamId) && GrantedSteamIds.Contains(steamId);
    }

    /// <summary>Lets a player use the menu. Only the host decides this.</summary>
    internal static bool Grant(string steamId)
    {
        if (!IsHost || string.IsNullOrEmpty(steamId) || !GrantedSteamIds.Add(steamId))
        {
            return false;
        }

        Plugin.Log.LogInfo($"Admin access granted to {steamId}.");
        Changed?.Invoke();
        return true;
    }

    internal static bool Revoke(string steamId)
    {
        if (!IsHost || string.IsNullOrEmpty(steamId) || !GrantedSteamIds.Remove(steamId))
        {
            return false;
        }

        Plugin.Log.LogInfo($"Admin access revoked from {steamId}.");
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Drops every grant. Called when a lobby is created, so access never carries over from
    /// one group of players to the next.
    /// </summary>
    internal static void Reset()
    {
        if (GrantedSteamIds.Count == 0)
        {
            return;
        }

        GrantedSteamIds.Clear();
        Changed?.Invoke();
    }

    private static string LocalSteamId()
    {
        PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
        return local == null ? string.Empty : SemiFunc.PlayerGetSteamID(local);
    }
}
