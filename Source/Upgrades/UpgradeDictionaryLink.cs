using System;
using System.Collections.Generic;
using AdminMenu.Catalog;
using AdminMenu.Interop;

namespace AdminMenu.Upgrades;

/// <summary>
/// Makes REPOLib's level table for a modded upgrade and the game's stat registry the same
/// object, which REPOLib itself fails to do.
///
/// REPOLib's <c>Upgrades.RegisterUpgrades</c> looks the upgrade's key up in
/// <c>StatsManager.dictionaryOfDictionaries</c> and, when it finds it, assigns the result to
/// a local variable instead of to <c>PlayerUpgrade.PlayerDictionary</c>. From then on there
/// are two tables for one upgrade, and they drift.
///
/// That costs more than a wrong number in the save file. The game's own
/// <c>PunManager.ReceiveSyncData</c> clears and refills the registry's tables in place, so
/// when the two are one object a host sync carries modded upgrade levels to every client and
/// REPOLib reads them; when they are not, a client's REPOLib keeps a table the host never
/// touches. Pointing REPOLib at the registry's table is what fixes both at once.
/// </summary>
internal static class UpgradeDictionaryLink
{
    private static readonly HashSet<string> Linked = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Links one upgrade. Vanilla upgrades need nothing - the game owns both sides - so they
    /// report success straight away. Cheap enough to call before every read and write.
    /// </summary>
    internal static bool Ensure(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId) || !RepoLibBridge.IsModdedUpgrade(upgradeId))
        {
            return true;
        }

        if (!RepoLibBridge.TryGetModdedUpgradeTable(upgradeId, out Dictionary<string, int> repoTable))
        {
            return false;
        }

        IDictionary<string, Dictionary<string, int>> registry =
            StatsDictionaryAccess.GetRegistry(StatsManager.instance);
        if (registry == null)
        {
            return false;
        }

        string statsKey = UpgradeCatalog.StatsKeyFor(upgradeId);

        if (!registry.TryGetValue(statsKey, out Dictionary<string, int> statsTable) || statsTable == null)
        {
            // Nothing registered yet: REPOLib's own table becomes the registry's.
            registry[statsKey] = repoTable;
            Report(upgradeId, "registered REPOLib's table with the game");
            return true;
        }

        if (ReferenceEquals(statsTable, repoTable))
        {
            return true;
        }

        // Two tables already exist. The registry's is the one the game syncs and saves, so it
        // wins; levels REPOLib knows about and it does not are carried over first.
        foreach (KeyValuePair<string, int> pair in repoTable)
        {
            if (!statsTable.ContainsKey(pair.Key))
            {
                statsTable[pair.Key] = pair.Value;
            }
        }

        if (!RepoLibBridge.TrySetModdedUpgradeTable(upgradeId, statsTable))
        {
            return false;
        }

        Report(upgradeId, "pointed REPOLib at the game's table");
        return true;
    }

    /// <summary>Links every modded upgrade. Called once before the upgrade pages are read.</summary>
    internal static void EnsureAll()
    {
        foreach (string upgradeId in RepoLibBridge.GetModdedUpgradeIds())
        {
            Ensure(upgradeId);
        }
    }

    /// <summary>Forgets what was reported, so a new run logs its repairs again.</summary>
    internal static void Reset()
    {
        Linked.Clear();
    }

    private static void Report(string upgradeId, string what)
    {
        if (Linked.Add(upgradeId))
        {
            Plugin.LogVerbose($"Upgrade \"{upgradeId}\": {what}.");
        }
    }
}
