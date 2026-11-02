using AdminMenu.Catalog;
using AdminMenu.Interop;
using AdminMenu.Targeting;
using UnityEngine;

namespace AdminMenu.Upgrades;

/// <summary>
/// Sets a player's upgrade to a level, and takes it away again.
///
/// Two paths, because the game and REPOLib own different upgrades:
///
/// A vanilla upgrade goes through <c>PunManager.TesterUpgradeCommandRPC</c>, which the game
/// authorises for the host and runs on every client. That RPC is delta-based, so an absolute
/// level becomes the difference from the current one; a negative difference runs the same
/// <c>UpgradePlayer*</c> method in reverse, which is what makes removal unwind the effect -
/// max health, stamina, grab range - instead of leaving it applied with a smaller number
/// beside it. The reference mod's route, the thirteen <c>Update*RightAway</c> methods, is
/// gone in v0.5.0: they are private now and themselves take a delta.
///
/// A modded upgrade goes through <c>REPOLib.PlayerUpgrade.SetLevel</c>, which runs the mod's
/// own upgrade action and raises REPOLib's networked event. The game's RPC cannot do this:
/// for a name it does not know, it only writes the dictionary and the mod's code never runs.
/// The level is then written once more through <c>StatsManager.DictionaryUpdateValue</c>,
/// which is not redundant - the game strips a <c>playerUpgrade*</c> entry whose value is 0,
/// so that call is what properly erases an upgrade rather than leaving a zero behind.
/// </summary>
internal static class UpgradeApplier
{
    /// <summary>
    /// Sets the player's level in this upgrade. Returns false when the change could not be
    /// made: not the host, no such upgrade, or the player has left.
    /// </summary>
    internal static bool SetLevel(string upgradeId, TargetPlayer player, int level)
    {
        if (string.IsNullOrEmpty(upgradeId) || !player.IsValid)
        {
            return false;
        }

        if (!PunBridge.CanAct)
        {
            Plugin.Log.LogWarning($"Only the host can change upgrades, so \"{upgradeId}\" was left alone.");
            return false;
        }

        level = Mathf.Max(level, 0);
        int current = UpgradeLevelReader.GetLevel(upgradeId, player);
        if (current == level)
        {
            return true;
        }

        bool applied = RepoLibBridge.IsModdedUpgrade(upgradeId)
            ? ApplyModded(upgradeId, player.SteamId, level)
            : ApplyVanilla(upgradeId, player.SteamId, level, current);

        if (!applied)
        {
            return false;
        }

        int reached = UpgradeLevelReader.GetLevel(upgradeId, player);
        if (reached != level)
        {
            Plugin.Log.LogWarning(
                $"Upgrade \"{upgradeId}\" for {player.Name} was set to {level} but reads back as {reached}.");
            return false;
        }

        Plugin.LogVerbose($"Upgrade \"{upgradeId}\" for {player.Name}: {current} -> {level}.");
        return true;
    }

    internal static bool AddLevel(string upgradeId, TargetPlayer player, int amount = 1)
    {
        return SetLevel(upgradeId, player, UpgradeLevelReader.GetLevel(upgradeId, player) + amount);
    }

    internal static bool RemoveLevel(string upgradeId, TargetPlayer player, int amount = 1)
    {
        return AddLevel(upgradeId, player, -amount);
    }

    /// <summary>Takes the upgrade away completely.</summary>
    internal static bool Clear(string upgradeId, TargetPlayer player)
    {
        return SetLevel(upgradeId, player, 0);
    }

    /// <summary>Takes every upgrade in the catalog away from this player.</summary>
    internal static int ClearAll(TargetPlayer player)
    {
        int cleared = 0;
        foreach (CatalogEntry<string> entry in UpgradeCatalog.All)
        {
            if (UpgradeLevelReader.GetLevel(entry.Id, player) > 0 && Clear(entry.Id, player))
            {
                cleared++;
            }
        }

        return cleared;
    }

    private static bool ApplyVanilla(string upgradeId, string steamId, int level, int current)
    {
        return PunBridge.ApplyVanillaUpgradeDelta(steamId, upgradeId, level - current);
    }

    private static bool ApplyModded(string upgradeId, string steamId, int level)
    {
        UpgradeDictionaryLink.Ensure(upgradeId);

        if (!RepoLibBridge.TrySetModdedUpgradeLevel(upgradeId, steamId, level))
        {
            return false;
        }

        // Through the game's own setter, so a level of 0 strips the entry instead of leaving
        // a zero in the save file, and so the value reaches clients on the next sync.
        StatsDictionaryAccess.SetValue(UpgradeCatalog.StatsKeyFor(upgradeId), steamId, level);
        PunBridge.SyncAllDictionaries();
        return true;
    }
}
