using System;
using System.Collections.Generic;

namespace AdminMenu.Catalog;

/// <summary>
/// Name search across a catalog. With a few content mods installed the item list runs past a
/// hundred entries, which no amount of category pages makes browsable, so the menu puts a
/// search box above the categories and filters with this.
/// </summary>
internal static class CatalogSearch
{
    /// <summary>
    /// Entries whose display name or internal id contains <paramref name="query"/>, ignoring
    /// case. An empty query returns everything, so the caller can feed the box straight in.
    /// Entries whose name starts with the query come first.
    /// </summary>
    internal static List<CatalogEntry<TPayload>> Filter<TPayload>(
        IEnumerable<CatalogEntry<TPayload>> entries,
        string query)
    {
        List<CatalogEntry<TPayload>> matches = new List<CatalogEntry<TPayload>>();
        if (entries == null)
        {
            return matches;
        }

        string trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            matches.AddRange(entries);
            return matches;
        }

        List<CatalogEntry<TPayload>> prefixMatches = new List<CatalogEntry<TPayload>>();

        foreach (CatalogEntry<TPayload> entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.DisplayName.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                prefixMatches.Add(entry);
            }
            else if (Contains(entry.DisplayName, trimmed) || Contains(entry.Id, trimmed))
            {
                matches.Add(entry);
            }
        }

        prefixMatches.AddRange(matches);
        return prefixMatches;
    }

    private static bool Contains(string haystack, string needle)
    {
        return !string.IsNullOrEmpty(haystack) &&
               haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
