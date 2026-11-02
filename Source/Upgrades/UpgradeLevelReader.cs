using AdminMenu.Catalog;
using AdminMenu.Interop;
using AdminMenu.Targeting;

namespace AdminMenu.Upgrades;

/// <summary>
/// The level a player has in an upgrade, read from whichever side owns it.
///
/// A vanilla upgrade lives in the game's stat registry. A modded one lives in REPOLib, which
/// after <see cref="UpgradeDictionaryLink"/> has run is the same table, so the two agree -
/// and when they do not, the registry wins, because that is what the game saves and syncs.
/// </summary>
internal static class UpgradeLevelReader
{
    internal static int GetLevel(string upgradeId, TargetPlayer player)
    {
        return player.IsValid ? GetLevel(upgradeId, player.SteamId) : 0;
    }

    internal static int GetLevel(string upgradeId, string steamId)
    {
        if (string.IsNullOrEmpty(upgradeId) || string.IsNullOrEmpty(steamId))
        {
            return 0;
        }

        UpgradeDictionaryLink.Ensure(upgradeId);

        int fromStats = StatsDictionaryAccess.GetValue(UpgradeCatalog.StatsKeyFor(upgradeId), steamId);

        if (!RepoLibBridge.IsModdedUpgrade(upgradeId))
        {
            return fromStats;
        }

        if (!RepoLibBridge.TryGetModdedUpgradeLevel(upgradeId, steamId, out int fromRepoLib))
        {
            return fromStats;
        }

        if (fromRepoLib != fromStats && StatsDictionaryAccess.ContainsTable(UpgradeCatalog.StatsKeyFor(upgradeId)))
        {
            Plugin.LogVerbose(
                $"Upgrade \"{upgradeId}\": REPOLib says {fromRepoLib}, the game says {fromStats}; " +
                "using the game's value.");
            return fromStats;
        }

        return fromRepoLib;
    }

    /// <summary>True when the player has this upgrade at all.</summary>
    internal static bool HasUpgrade(string upgradeId, TargetPlayer player)
    {
        return GetLevel(upgradeId, player) > 0;
    }
}
