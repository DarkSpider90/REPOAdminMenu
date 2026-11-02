using System;
using System.Collections.Generic;

namespace AdminMenu.Catalog;

/// <summary>
/// Turns a flat entry list into the category pages the menu shows. Every catalog groups the
/// same way, so the rule lives here once: categories in the order the game's enum declares
/// them, entries inside a category by name, and vanilla before modded where names tie.
/// </summary>
internal static class CatalogGrouping
{
    internal static List<CatalogGroup<TPayload>> Build<TPayload>(List<CatalogEntry<TPayload>> entries)
    {
        List<CatalogGroup<TPayload>> groups = new List<CatalogGroup<TPayload>>();
        if (entries == null || entries.Count == 0)
        {
            return groups;
        }

        Dictionary<string, List<CatalogEntry<TPayload>>> byCategory =
            new Dictionary<string, List<CatalogEntry<TPayload>>>(StringComparer.Ordinal);
        Dictionary<string, int> order = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (CatalogEntry<TPayload> entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (!byCategory.TryGetValue(entry.Category, out List<CatalogEntry<TPayload>> bucket))
            {
                bucket = new List<CatalogEntry<TPayload>>();
                byCategory[entry.Category] = bucket;
                order[entry.Category] = entry.CategoryOrder;
            }

            bucket.Add(entry);
        }

        foreach (KeyValuePair<string, List<CatalogEntry<TPayload>>> pair in byCategory)
        {
            pair.Value.Sort(CompareEntries);
            groups.Add(new CatalogGroup<TPayload>(pair.Key, order[pair.Key], pair.Value));
        }

        groups.Sort(CompareGroups);
        return groups;
    }

    private static int CompareEntries<TPayload>(CatalogEntry<TPayload> left, CatalogEntry<TPayload> right)
    {
        int byName = string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase);
        return byName != 0 ? byName : left.Source.CompareTo(right.Source);
    }

    private static int CompareGroups<TPayload>(CatalogGroup<TPayload> left, CatalogGroup<TPayload> right)
    {
        int byOrder = left.Order.CompareTo(right.Order);
        return byOrder != 0 ? byOrder : string.Compare(left.Category, right.Category, StringComparison.Ordinal);
    }
}
