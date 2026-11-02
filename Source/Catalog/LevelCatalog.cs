using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// Every level the run can be sent to, grouped the way the game keeps them.
///
/// <c>RunManager</c> holds three lists - the normal levels, the shops and the arenas - plus a
/// handful of one-off levels in fields of their own. Modded levels are registered into the
/// normal list by REPOLib, so they appear without anything special here; REPOLib's own list
/// of registered levels is only used to mark them as modded in the menu.
///
/// The display name is the level's localized narrative name where it has one, because
/// "Level - Manor" is not what the player sees anywhere else in the game.
/// </summary>
internal static class LevelCatalog
{
    private const string CategoryLevels = "Levels";
    private const string CategoryShops = "Shops";
    private const string CategoryArenas = "Arenas";
    private const string CategorySpecial = "Special";

    private static readonly List<CatalogEntry<Level>> Entries = new List<CatalogEntry<Level>>();
    private static List<CatalogGroup<Level>> _groups = new List<CatalogGroup<Level>>();

    private static readonly CatalogState State = new CatalogState("levels", Rebuild);

    internal static IReadOnlyList<CatalogEntry<Level>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<Level>> Groups
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
        _groups = new List<CatalogGroup<Level>>();

        RunManager runManager = RunManager.instance;
        if (runManager == null)
        {
            return;
        }

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<Level> modded = CollectModded();

        AddList(runManager.levels, CategoryLevels, 0, modded, seen);
        AddList(runManager.levelShop, CategoryShops, 1, modded, seen);
        AddList(runManager.levelArena, CategoryArenas, 2, modded, seen);

        // One-off levels the game keeps in fields rather than in a list.
        Add(runManager.levelLobby, CategorySpecial, 3, modded, seen);
        Add(runManager.levelTutorial, CategorySpecial, 3, modded, seen);
        Add(runManager.levelRecording, CategorySpecial, 3, modded, seen);

        TellDuplicatesApart();

        _groups = CatalogGrouping.Build(Entries);
        Plugin.LogVerbose($"Level catalog: {Entries.Count} levels in {_groups.Count} groups.");
    }

    /// <summary>
    /// Adds the asset name to levels that share a narrative name.
    ///
    /// All five shops are called the same thing in game - they are the same place as far as
    /// the story is concerned - so a list of them read as the same line five times over and
    /// said nothing about which one was about to be loaded. Where a name is unique it is left
    /// alone; where it is not, every one of them gets its own name added.
    /// </summary>
    private static void TellDuplicatesApart()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

        foreach (CatalogEntry<Level> entry in Entries)
        {
            counts.TryGetValue(entry.DisplayName, out int seen);
            counts[entry.DisplayName] = seen + 1;
        }

        for (int i = 0; i < Entries.Count; i++)
        {
            CatalogEntry<Level> entry = Entries[i];
            if (counts[entry.DisplayName] < 2)
            {
                continue;
            }

            string asset = GameText.FromPrefabName(entry.Id);
            if (string.IsNullOrWhiteSpace(asset) ||
                string.Equals(asset, entry.DisplayName, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            Entries[i] = new CatalogEntry<Level>(
                entry.Id,
                $"{entry.DisplayName} - {asset}",
                entry.Category,
                entry.CategoryOrder,
                entry.Source,
                entry.Payload);
        }
    }

    internal static bool TryGet(string id, out CatalogEntry<Level> entry)
    {
        foreach (CatalogEntry<Level> candidate in Entries)
        {
            if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
    }

    private static HashSet<Level> CollectModded()
    {
        HashSet<Level> modded = new HashSet<Level>();
        foreach (Level level in RepoLibBridge.GetRegisteredLevels())
        {
            if (level != null)
            {
                modded.Add(level);
            }
        }

        return modded;
    }

    private static void AddList(
        List<Level> levels,
        string category,
        int order,
        HashSet<Level> modded,
        HashSet<string> seen)
    {
        if (levels == null)
        {
            return;
        }

        foreach (Level level in levels)
        {
            Add(level, category, order, modded, seen);
        }
    }

    private static void Add(Level level, string category, int order, HashSet<Level> modded, HashSet<string> seen)
    {
        if (level == null || string.IsNullOrEmpty(level.name) || !seen.Add(level.name))
        {
            return;
        }

        string displayName = GameText.Resolve(level.NarrativeNameLocalized, level.NarrativeName);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = GameText.FromPrefabName(level.name);
        }

        Entries.Add(new CatalogEntry<Level>(
            level.name,
            displayName,
            category,
            order,
            modded.Contains(level) ? ContentSource.Mod : ContentSource.Vanilla,
            level));
    }
}
