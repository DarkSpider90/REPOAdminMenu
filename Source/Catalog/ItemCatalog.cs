using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// Every shop item that can be spawned, grouped by the game's own <c>SemiFunc.itemType</c>.
///
/// The game's <c>itemDictionary</c> is not a complete list on its own: <c>LoadItemsFromFolder</c>
/// refills it from Resources, which drops modded items until REPOLib puts them back, so the
/// catalog is the union of that dictionary and REPOLib's registered items. Keys are the
/// asset name, the same key the game's dictionary uses.
/// </summary>
internal static class ItemCatalog
{
    private static readonly List<CatalogEntry<Item>> Entries = new List<CatalogEntry<Item>>();
    private static List<CatalogGroup<Item>> _groups = new List<CatalogGroup<Item>>();

    private static readonly CatalogState State = new CatalogState("items", Rebuild);

    internal static IReadOnlyList<CatalogEntry<Item>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<Item>> Groups
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
        _groups = new List<CatalogGroup<Item>>();

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> moddedIds = CollectModdedIds();

        StatsManager statsManager = StatsManager.instance;
        if (statsManager?.itemDictionary != null)
        {
            foreach (KeyValuePair<string, Item> pair in statsManager.itemDictionary)
            {
                Add(pair.Value, moddedIds, seen);
            }
        }

        // Items REPOLib knows about but the dictionary has lost, which happens between a
        // reload and REPOLib's restore pass.
        foreach (Item item in RepoLibBridge.GetRegisteredItems())
        {
            Add(item, moddedIds, seen);
        }

        _groups = CatalogGrouping.Build(Entries);
        Plugin.LogVerbose($"Item catalog: {Entries.Count} items in {_groups.Count} categories.");
    }

    internal static bool TryGet(string id, out CatalogEntry<Item> entry)
    {
        foreach (CatalogEntry<Item> candidate in Entries)
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

    private static HashSet<string> CollectModdedIds()
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Item item in RepoLibBridge.GetRegisteredItems())
        {
            if (item != null && !string.IsNullOrEmpty(item.name))
            {
                ids.Add(item.name);
            }
        }

        return ids;
    }

    private static void Add(Item item, HashSet<string> moddedIds, HashSet<string> seen)
    {
        if (item == null || item.disabled || string.IsNullOrEmpty(item.name) || !seen.Add(item.name))
        {
            return;
        }

        Entries.Add(new CatalogEntry<Item>(
            item.name,
            GameText.Resolve(item.itemNameLocalized, item.itemName),
            GameText.Humanize(item.itemType.ToString()),
            (int)item.itemType,
            moddedIds.Contains(item.name) ? ContentSource.Mod : ContentSource.Vanilla,
            item));
    }
}
