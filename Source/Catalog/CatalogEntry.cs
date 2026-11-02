namespace AdminMenu.Catalog;

/// <summary>
/// One spawnable or applicable thing, with everything the menu needs to show it and the
/// payload the action needs to use it.
///
/// <see cref="Category"/> and <see cref="CategoryOrder"/> come from the game's own grouping
/// enum - <c>SemiFunc.itemType</c> for items, <c>ValuableVolume.Type</c> for valuables,
/// <c>SemiFunc.Rarity</c> for cosmetic boxes - so new game categories show up by themselves
/// and in the order the game declares them.
/// </summary>
internal sealed class CatalogEntry<TPayload>
{
    internal CatalogEntry(
        string id,
        string displayName,
        string category,
        int categoryOrder,
        ContentSource source,
        TPayload payload)
    {
        Id = id ?? string.Empty;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        Category = category ?? string.Empty;
        CategoryOrder = categoryOrder;
        Source = source;
        Payload = payload;
    }

    /// <summary>Stable key: the game's own internal name. Unique within a catalog.</summary>
    internal string Id { get; }

    /// <summary>Localized name, or the best fallback the game offered.</summary>
    internal string DisplayName { get; }

    internal string Category { get; }

    /// <summary>Declaration order of the category in the game's enum.</summary>
    internal int CategoryOrder { get; }

    internal ContentSource Source { get; }

    internal TPayload Payload { get; }

    internal bool IsModded => Source == ContentSource.Mod;

    public override string ToString()
    {
        return IsModded ? $"{DisplayName} (mod)" : DisplayName;
    }
}
