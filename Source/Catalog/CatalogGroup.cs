using System.Collections.Generic;

namespace AdminMenu.Catalog;

/// <summary>
/// One category of a catalog with its entries, already sorted. The menu turns a group into
/// a page, which is why the grouping is built once per level rather than in the UI.
/// </summary>
internal sealed class CatalogGroup<TPayload>
{
    internal CatalogGroup(string category, int order, List<CatalogEntry<TPayload>> entries)
    {
        Category = category ?? string.Empty;
        Order = order;
        Entries = entries ?? new List<CatalogEntry<TPayload>>();
    }

    internal string Category { get; }

    internal int Order { get; }

    internal IReadOnlyList<CatalogEntry<TPayload>> Entries { get; }

    internal int Count => Entries.Count;
}
