using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// Every player upgrade, vanilla and modded, with the name the game shows for it.
///
/// Nothing here is a list in code. The game registers all thirteen vanilla upgrades into
/// <c>StatsManager.upgradesInfo</c> in <c>Start</c>, complete with a localized display name,
/// and into the stat registry under a <c>playerUpgrade*</c> key; REPOLib registers modded
/// ones under the same key shape. The catalog is the union of the three, which is why an
/// upgrade added by a game patch or by another mod needs no change in this mod.
///
/// The payload is the stat registry key, because that is what a level read needs. Whether an
/// upgrade is modded decides how it is applied, and that is <see cref="CatalogEntry{T}.Source"/>.
/// </summary>
internal static class UpgradeCatalog
{
    internal const string StatsKeyPrefix = "playerUpgrade";

    private static readonly List<CatalogEntry<string>> Entries = new List<CatalogEntry<string>>();
    private static List<CatalogGroup<string>> _groups = new List<CatalogGroup<string>>();

    private static readonly CatalogState State = new CatalogState("upgrades", Rebuild);

    internal static IReadOnlyList<CatalogEntry<string>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<string>> Groups
    {
        get
        {
            State.EnsureBuilt();
            return _groups;
        }
    }

    /// <summary>Builds the list if the game moved on since the last time it was read.</summary>
    internal static void EnsureBuilt()
    {
        State.EnsureBuilt();
    }

    /// <summary>Forces the next read to rebuild.</summary>
    internal static void Invalidate()
    {
        State.Invalidate();
    }

    private static void Rebuild()
    {
        Entries.Clear();
        _groups = new List<CatalogGroup<string>>();

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        AddFromUpgradesInfo(seen);
        AddFromStatsRegistry(seen);
        AddFromRepoLib(seen);

        _groups = CatalogGrouping.Build(Entries);
        Report();
    }

    /// <summary>
    /// Lists what was found and how each entry was classed.
    ///
    /// Whether an upgrade counts as modded decides how it is applied, not just how it is
    /// labelled: a modded one has to go through REPOLib, because the game's own upgrade RPC
    /// does nothing for a name it does not know. So when the split looks wrong, this is the
    /// line to read - turn on VerboseLogging and every upgrade says which side it landed on.
    /// </summary>
    private static void Report()
    {
        Plugin.LogVerbose($"Upgrade catalog: {Entries.Count} upgrades.");

        foreach (CatalogEntry<string> entry in Entries)
        {
            Plugin.LogVerbose($"  {entry.Id} -> \"{entry.DisplayName}\" ({entry.Source})");
        }
    }

    internal static bool TryGet(string upgradeId, out CatalogEntry<string> entry)
    {
        foreach (CatalogEntry<string> candidate in Entries)
        {
            if (string.Equals(candidate.Id, upgradeId, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    /// <summary>The stat registry key for an upgrade id, which is the id behind a fixed prefix.</summary>
    internal static string StatsKeyFor(string upgradeId)
    {
        return StatsKeyPrefix + upgradeId;
    }

    private static void AddFromUpgradesInfo(HashSet<string> seen)
    {
        Dictionary<string, StatsManager.UpgradeInfo> info = StatsManager.instance?.upgradesInfo;
        if (info == null)
        {
            return;
        }

        foreach (KeyValuePair<string, StatsManager.UpgradeInfo> pair in info)
        {
            string upgradeId = StripPrefix(pair.Key);
            if (upgradeId == null)
            {
                continue;
            }

            string displayName = pair.Value == null
                ? string.Empty
                : GameText.Resolve(pair.Value.displayNameLocalized, pair.Value.displayName);

            Add(upgradeId, displayName, seen);
        }
    }

    private static void AddFromStatsRegistry(HashSet<string> seen)
    {
        IDictionary<string, Dictionary<string, int>> registry =
            StatsDictionaryAccess.GetRegistry(StatsManager.instance);
        if (registry == null)
        {
            return;
        }

        // A copy, because resolving a name can touch the game and the registry is live.
        List<string> keys = new List<string>(registry.Keys);
        foreach (string key in keys)
        {
            string upgradeId = StripPrefix(key);
            if (upgradeId != null)
            {
                Add(upgradeId, string.Empty, seen);
            }
        }
    }

    private static void AddFromRepoLib(HashSet<string> seen)
    {
        foreach (string upgradeId in RepoLibBridge.GetModdedUpgradeIds())
        {
            Add(upgradeId, string.Empty, seen);
        }
    }

    /// <summary>
    /// Adds the upgrade unless it is already in. The display name falls back a step at a
    /// time: what the game or the mod wrote into <c>upgradesInfo</c>, then the name of the
    /// shop item REPOLib registered it with, then the id spaced out.
    /// </summary>
    private static void Add(string upgradeId, string displayName, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(upgradeId) || !seen.Add(upgradeId))
        {
            return;
        }

        bool modded = RepoLibBridge.IsModdedUpgrade(upgradeId);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = ResolveNameFromInfo(upgradeId);
        }

        if (string.IsNullOrWhiteSpace(displayName) && modded)
        {
            displayName = RepoLibBridge.GetModdedUpgradeItemName(upgradeId);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = GameText.Humanize(upgradeId);
        }

        Entries.Add(new CatalogEntry<string>(
            upgradeId,
            displayName,
            Strings.Get(StringKeys.CategoryUpgrades),
            0,
            modded ? ContentSource.Mod : ContentSource.Vanilla,
            StatsKeyFor(upgradeId)));
    }

    private static string ResolveNameFromInfo(string upgradeId)
    {
        Dictionary<string, StatsManager.UpgradeInfo> info = StatsManager.instance?.upgradesInfo;
        if (info == null || !info.TryGetValue(StatsKeyFor(upgradeId), out StatsManager.UpgradeInfo entry) || entry == null)
        {
            return string.Empty;
        }

        return GameText.Resolve(entry.displayNameLocalized, entry.displayName);
    }

    /// <summary>The id behind the key's prefix, or null when the key is not an upgrade key.</summary>
    private static string StripPrefix(string statsKey)
    {
        if (string.IsNullOrEmpty(statsKey) ||
            !statsKey.StartsWith(StatsKeyPrefix, StringComparison.Ordinal) ||
            statsKey.Length == StatsKeyPrefix.Length)
        {
            return null;
        }

        return statsKey.Substring(StatsKeyPrefix.Length);
    }
}
