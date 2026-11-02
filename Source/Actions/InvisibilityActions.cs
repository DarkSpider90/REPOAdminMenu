using System;
using System.Collections.Generic;
using System.Reflection;
using AdminMenu.Access;
using AdminMenu.Interop;
using AdminMenu.Targeting;
using HarmonyLib;

namespace AdminMenu.Actions;

/// <summary>
/// Makes a player invisible to the enemies, and says whether they already are.
///
/// This feature needs no patch of any kind. The game keeps a list of steam ids in
/// <c>EnemyDirector.debugNoVision</c> and checks it exactly where it matters: in the vision
/// loop that scans for players, in <c>Enemy.SetChaseTarget</c>, and in the Thin Man's own
/// look-up. So a player on that list is never picked as a target in the first place, rather
/// than being picked and then wrestled free - which is what the reference mod did with
/// forty-two patches that rewrote enemy state from outside and crashed the enemies whose
/// classes it had guessed wrong.
///
/// The list is filled through <c>PunManager.TesterNoAggroCommandRPC</c>, which the game
/// authorises for the host and applies on every client.
/// </summary>
internal static class InvisibilityActions
{
    private static readonly FieldInfo NoVisionField =
        AccessTools.Field(typeof(EnemyDirector), "debugNoVision");

    private static bool _reportedMissingList;

    internal static bool IsInvisible(TargetPlayer player)
    {
        if (!player.IsValid)
        {
            return false;
        }

        List<string> list = NoVisionList();
        return list != null && list.Contains(player.SteamId);
    }

    internal static bool SetInvisible(TargetPlayer player, bool state)
    {
        if (!player.IsValid)
        {
            return false;
        }

        if (!AdminAccess.CanAct)
        {
            Plugin.Log.LogWarning("Only the host can hide a player from the enemies.");
            return false;
        }

        if (!PunBridge.SetNoAggro(player.SteamId, state))
        {
            return false;
        }

        Plugin.LogVerbose($"Invisibility for {player.Name}: {state}.");
        return true;
    }

    private static List<string> NoVisionList()
    {
        EnemyDirector director = EnemyDirector.instance;
        if (director == null)
        {
            return null;
        }

        if (NoVisionField == null)
        {
            Report();
            return null;
        }

        try
        {
            return NoVisionField.GetValue(director) as List<string>;
        }
        catch (Exception ex)
        {
            Report(ex);
            return null;
        }
    }

    private static void Report(Exception ex = null)
    {
        if (_reportedMissingList)
        {
            return;
        }

        _reportedMissingList = true;
        Plugin.Log.LogWarning(
            "EnemyDirector.debugNoVision could not be read, so the menu cannot show who is " +
            "already invisible. " + (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
