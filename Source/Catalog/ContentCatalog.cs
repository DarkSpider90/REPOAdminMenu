namespace AdminMenu.Catalog;

/// <summary>
/// The catalogs as a set: somewhere to tell all of them at once that the world changed.
///
/// Reading goes to the catalog itself, which builds on demand. This exists for the other
/// direction - when a run ends, every list is stale and the whole set is dropped in one call.
/// </summary>
internal static class ContentCatalog
{
    /// <summary>Forces every catalog to rebuild the next time it is read.</summary>
    internal static void Invalidate()
    {
        ItemCatalog.Invalidate();
        ValuableCatalog.Invalidate();
        EnemyCatalog.Invalidate();
        CosmeticBoxCatalog.Invalidate();
        UpgradeCatalog.Invalidate();
        LevelCatalog.Invalidate();
    }
}
