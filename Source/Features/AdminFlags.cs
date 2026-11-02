using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Targeting;

namespace AdminMenu.Features;

/// <summary>
/// The on/off state behind the menu's switches.
///
/// There are two kinds here, and the difference is who does the work.
///
/// Run-wide switches - one-shot enemies, never-draining batteries, free shop items - are the
/// host changing what it simulates, so one flag it reads while it runs the game covers every
/// player and nobody else needs the mod.
///
/// Per-player abilities are granted rather than applied. Stamina and flight happen on that
/// player's own machine and cannot be done to them from outside, so the host records who has
/// been granted what and <see cref="AdminNetwork"/> tells them; their own copy of the mod
/// switches it on. A player without the mod never hears it and nothing breaks - the switch
/// stays on here, which is accurate, because what was given was permission.
///
/// Immortality is both: the host blocks the damage it works out itself, and the granted player
/// blocks what their own machine works out, such as a fall.
///
/// Invisibility is not here at all - the game has its own list for it, so
/// <see cref="PunBridge.SetNoAggro"/> is the whole feature and the state lives where the game
/// can see it.
/// </summary>
internal static class AdminFlags
{
    /// <summary>Who the host has granted each ability to, by steam id.</summary>
    private static readonly Dictionary<AdminFlag, HashSet<string>> Granted =
        new Dictionary<AdminFlag, HashSet<string>>();

    /// <summary>Raised when any switch changes, so an open menu page can redraw.</summary>
    internal static event Action Changed;

    /// <summary>Every enemy dies to one hit. Host-side, so it holds for the whole lobby.</summary>
    internal static bool OneShotEnemies { get; private set; }

    /// <summary>Shop items cost nothing. Host-side, so it holds for the whole lobby.</summary>
    internal static bool FreeShopItems { get; private set; }

    /// <summary>Batteries never drain. Host-side, so it holds for the whole lobby.</summary>
    internal static bool InfiniteAmmo { get; private set; }

    /// <summary>Stamina never drains, for the player at this machine.</summary>
    internal static bool LocalInfiniteStamina { get; private set; }

    /// <summary>True for the abilities a player has to switch on for themselves.</summary>
    internal static bool IsGrantedAbility(AdminFlag flag)
    {
        return flag == AdminFlag.Immortal || flag == AdminFlag.InfiniteStamina || flag == AdminFlag.FreeFlight;
    }

    internal static bool IsImmortal(string steamId)
    {
        return Has(AdminFlag.Immortal, steamId);
    }

    internal static bool IsImmortal(PlayerAvatar avatar)
    {
        return avatar != null && IsImmortal(SemiFunc.PlayerGetSteamID(avatar));
    }

    /// <summary>Whether this player has been granted an ability.</summary>
    internal static bool Has(AdminFlag flag, TargetPlayer player)
    {
        return player.IsValid && Has(flag, player.SteamId);
    }

    private static bool Has(AdminFlag flag, string steamId)
    {
        return !string.IsNullOrEmpty(steamId) &&
               Granted.TryGetValue(flag, out HashSet<string> set) &&
               set.Contains(steamId);
    }

    /// <summary>
    /// Grants or withdraws an ability for one player. The record is kept here, the player is
    /// told, and if the player is the one at this machine it also takes effect right away
    /// rather than going out over the network and back.
    /// </summary>
    internal static bool Grant(AdminFlag flag, TargetPlayer player, bool state)
    {
        if (!player.IsValid)
        {
            return false;
        }

        if (!Granted.TryGetValue(flag, out HashSet<string> set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            Granted[flag] = set;
        }

        bool changed = state ? set.Add(player.SteamId) : set.Remove(player.SteamId);
        if (!changed)
        {
            return true;
        }

        if (TargetPlayerResolver.IsLocal(player))
        {
            ApplyLocally(flag, state);
        }
        else
        {
            AdminNetwork.Send(player.SteamId, flag, state);
        }

        Plugin.LogVerbose($"{flag} for {player.Name}: {state}.");
        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Switches an ability on for the player at this machine. Called when the host grants it to
    /// themselves, and when a message arrives saying the host granted it to us.
    /// </summary>
    internal static void ApplyLocally(AdminFlag flag, bool state)
    {
        switch (flag)
        {
            case AdminFlag.Immortal:
                // The host already blocks the damage it works out; this covers what this
                // machine decides on its own, a fall for instance.
                PlayerAvatarAccess.SetGodMode(SemiFunc.PlayerAvatarLocal(), state);
                return;

            case AdminFlag.InfiniteStamina:
                LocalInfiniteStamina = state;
                return;

            case AdminFlag.FreeFlight:
                FreeFlight.SetActive(state);
                return;

            default:
                Plugin.Log.LogWarning($"{flag} is not something a player switches on for themselves.");
                return;
        }
    }

    internal static void SetOneShotEnemies(bool state)
    {
        if (OneShotEnemies == state)
        {
            return;
        }

        OneShotEnemies = state;
        Plugin.LogVerbose($"One-shot enemies: {state}.");
        Changed?.Invoke();
    }

    internal static void SetFreeShopItems(bool state)
    {
        if (FreeShopItems == state)
        {
            return;
        }

        FreeShopItems = state;
        Plugin.LogVerbose($"Free shop items: {state}.");
        Changed?.Invoke();
    }

    internal static void SetInfiniteAmmo(bool state)
    {
        if (InfiniteAmmo == state)
        {
            return;
        }

        InfiniteAmmo = state;
        Plugin.LogVerbose($"Infinite ammo: {state}.");
        Changed?.Invoke();
    }

    /// <summary>
    /// Clears everything. Called when a run ends, so a switch set for one lobby does not carry
    /// into the next.
    /// </summary>
    internal static void Reset()
    {
        bool had = Granted.Count > 0 || OneShotEnemies || FreeShopItems || InfiniteAmmo || LocalInfiniteStamina;

        FreeFlight.SetActive(false);
        LocalInfiniteStamina = false;

        Granted.Clear();
        OneShotEnemies = false;
        FreeShopItems = false;
        InfiniteAmmo = false;

        if (had)
        {
            Changed?.Invoke();
        }
    }
}
