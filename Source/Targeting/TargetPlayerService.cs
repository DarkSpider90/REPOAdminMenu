using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdminMenu.Targeting;

/// <summary>
/// The single answer to "who are we acting on right now".
///
/// Every action and every spawn in the menu goes through here, which is the point: the target
/// is not a parameter each page passes around and occasionally forgets, it is one piece of
/// state with one fallback rule. Nothing is selected to begin with, and then the target is
/// the local player - or, once the host is dead, whoever they are spectating. A player who
/// leaves the lobby stops being the target by themselves, because the selection is a steam id
/// that no longer resolves.
/// </summary>
internal static class TargetPlayerService
{
    private static TargetPlayer _selected = TargetPlayer.None;

    /// <summary>Raised when the target changes, so an open menu page can relabel itself.</summary>
    internal static event Action Changed;

    /// <summary>
    /// The target as of now. Falls back to the local player, or to the player the local
    /// player is spectating, whenever nothing is selected or the selection has left.
    /// </summary>
    internal static TargetPlayer Current
    {
        get
        {
            if (_selected.IsValid && _selected.TryResolve(out _))
            {
                return _selected;
            }

            return TargetPlayerResolver.LocalOrSpectated();
        }
    }

    /// <summary>True while the target is the menu's fallback rather than an explicit pick.</summary>
    internal static bool IsFollowingLocal => !_selected.IsValid || !_selected.TryResolve(out _);

    internal static void Select(TargetPlayer player)
    {
        if (_selected.Equals(player))
        {
            return;
        }

        _selected = player;
        Plugin.LogVerbose($"Admin target is now {Current}.");
        Changed?.Invoke();
    }

    internal static void Select(PlayerAvatar avatar)
    {
        Select(TargetPlayer.From(avatar));
    }

    /// <summary>Goes back to the fallback: the local player, or whoever they are spectating.</summary>
    internal static void FollowLocal()
    {
        Select(TargetPlayer.None);
    }

    /// <summary>Drops the selection without announcing it. Called when a run ends.</summary>
    internal static void Reset()
    {
        _selected = TargetPlayer.None;
    }

    internal static List<TargetPlayer> ListPlayers()
    {
        return TargetPlayerResolver.ListPlayers();
    }

    internal static bool TryResolveCurrent(out PlayerAvatar avatar)
    {
        return Current.TryResolve(out avatar);
    }

    /// <summary>Where something spawned by the menu should appear for the current target.</summary>
    internal static bool TryGetSpawnPoint(out Vector3 position)
    {
        return TargetPlayerResolver.TryGetSpawnPoint(Current, out position);
    }
}
