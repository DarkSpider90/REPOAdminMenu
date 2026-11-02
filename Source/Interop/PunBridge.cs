using System;
using Photon.Pun;
using UnityEngine;

namespace AdminMenu.Interop;

/// <summary>
/// Sends admin actions through the game's own networked entry points.
///
/// R.E.P.O. already ships master-authorised RPCs for everything this mod needs to change on
/// another player, so the mod opens no channel of its own: one call reaches every client and
/// the game's own code decides what the change means. Each wrapper also covers singleplayer,
/// where the RPC method is simply invoked in place, exactly as
/// <c>DebugCommandHandler</c> does.
/// </summary>
internal static class PunBridge
{
    /// <summary>True when this client is allowed to change the run at all.</summary>
    internal static bool CanAct => SemiFunc.IsMasterClientOrSingleplayer();

    /// <summary>
    /// Moves a vanilla upgrade by <paramref name="delta"/> levels.
    ///
    /// <c>TesterUpgradeCommandRPC</c> is delta-based, not absolute: it routes the thirteen
    /// vanilla names to the matching <c>UpgradePlayer*</c> method, which applies the effect
    /// and keeps <c>StatsManager</c> in step. A negative delta unwinds the effect through the
    /// same code path, so removal is as reliable as granting.
    /// </summary>
    internal static bool ApplyVanillaUpgradeDelta(string steamId, string upgradeName, int delta)
    {
        if (string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(upgradeName) || delta == 0)
        {
            return false;
        }

        return SendRpc("TesterUpgradeCommandRPC", steamId, upgradeName, delta);
    }

    /// <summary>
    /// Adds the player to, or removes them from, the game's own no-vision list.
    ///
    /// <c>EnemyDirector.debugNoVision</c> is checked where enemies pick targets, so this
    /// suppresses target selection without touching enemy state from outside and needs no
    /// patch of any kind.
    /// </summary>
    internal static bool SetNoAggro(string steamId, bool state)
    {
        if (string.IsNullOrEmpty(steamId))
        {
            return false;
        }

        return SendRpc("TesterNoAggroCommandRPC", steamId, state);
    }

    /// <summary>Sets a run-wide stat, money included. Master-gated by the game itself.</summary>
    internal static bool SetRunStat(string statName, int value)
    {
        if (string.IsNullOrEmpty(statName) || PunManager.instance == null)
        {
            return false;
        }

        try
        {
            PunManager.instance.SetRunStatSet(statName, value);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not set run stat '{statName}' to {value}. {ex}");
            return false;
        }
    }

    /// <summary>
    /// Pushes the whole stat registry to the other clients. The game calls this at the points
    /// where it matters; the mod calls it after writing a dictionary the game does not know
    /// how to sync on its own.
    /// </summary>
    internal static void SyncAllDictionaries()
    {
        if (PunManager.instance == null || !SemiFunc.IsMasterClientOrSingleplayer())
        {
            return;
        }

        try
        {
            PunManager.instance.SyncAllDictionaries();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not sync the stat dictionaries. {ex}");
        }
    }

    /// <summary>
    /// Calls a <c>PunManager</c> RPC on everyone in multiplayer, or straight through in
    /// singleplayer where there is no network to carry it.
    /// </summary>
    private static bool SendRpc(string methodName, params object[] args)
    {
        PunManager punManager = PunManager.instance;
        if (punManager == null)
        {
            Plugin.Log.LogWarning($"No PunManager yet, so '{methodName}' was dropped.");
            return false;
        }

        try
        {
            if (!SemiFunc.IsMultiplayer())
            {
                InvokeLocally(punManager, methodName, args);
                return true;
            }

            // photonView is internal to the game assembly; the component itself is not.
            PhotonView photonView = punManager.GetComponent<PhotonView>();
            if (photonView == null)
            {
                Plugin.Log.LogWarning($"PunManager carries no PhotonView, so '{methodName}' was dropped.");
                return false;
            }

            photonView.RPC(methodName, RpcTarget.All, args);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"RPC '{methodName}' failed. {ex}");
            return false;
        }
    }

    private static void InvokeLocally(PunManager punManager, string methodName, object[] args)
    {
        switch (methodName)
        {
            case "TesterUpgradeCommandRPC":
                punManager.TesterUpgradeCommandRPC((string)args[0], (string)args[1], (int)args[2]);
                return;
            case "TesterNoAggroCommandRPC":
                punManager.TesterNoAggroCommandRPC((string)args[0], (bool)args[1]);
                return;
            default:
                Plugin.Log.LogError($"'{methodName}' has no singleplayer path, so nothing happened.");
                return;
        }
    }
}
