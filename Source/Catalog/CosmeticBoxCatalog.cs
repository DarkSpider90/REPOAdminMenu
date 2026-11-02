using System;
using System.Collections.Generic;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// The cosmetic boxes, one per rarity.
///
/// <c>ValuableDirector.cosmeticWorldObjectSetups</c> is indexed by <c>SemiFunc.Rarity</c> -
/// the game itself reads it as <c>setups[(int)rarity]</c> - so the list is read through the
/// rarity enum rather than by position, and a rarity the game adds later appears on its own
/// as long as the director has a setup for it.
/// </summary>
internal static class CosmeticBoxCatalog
{
    private static readonly List<CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup>> Entries =
        new List<CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup>>();

    private static List<CatalogGroup<ValuableDirector.CosmeticWorldObjectSetup>> _groups =
        new List<CatalogGroup<ValuableDirector.CosmeticWorldObjectSetup>>();

    private static readonly CatalogState State = new CatalogState("cosmetic boxes", Rebuild);

    internal static IReadOnlyList<CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<ValuableDirector.CosmeticWorldObjectSetup>> Groups
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
        _groups = new List<CatalogGroup<ValuableDirector.CosmeticWorldObjectSetup>>();

        List<ValuableDirector.CosmeticWorldObjectSetup> setups = ValuableDirector.instance?.cosmeticWorldObjectSetups;
        if (setups == null)
        {
            return;
        }

        foreach (SemiFunc.Rarity rarity in Rarities)
        {
            int index = (int)rarity;
            if (index < 0 || index >= setups.Count)
            {
                continue;
            }

            ValuableDirector.CosmeticWorldObjectSetup setup = setups[index];
            if (setup?.prefab == null)
            {
                continue;
            }

            Entries.Add(new CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup>(
                rarity.ToString(),
                Strings.ForRarity(rarity),
                Strings.Get(StringKeys.CategoryCosmeticBoxes),
                0,
                ContentSource.Vanilla,
                setup));
        }

        _groups = CatalogGrouping.Build(Entries);
        Plugin.LogVerbose($"Cosmetic box catalog: {Entries.Count} rarities.");
    }

    internal static bool TryGet(SemiFunc.Rarity rarity, out CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup> entry)
    {
        string id = rarity.ToString();
        foreach (CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup> candidate in Entries)
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

    private static readonly SemiFunc.Rarity[] Rarities = (SemiFunc.Rarity[])Enum.GetValues(typeof(SemiFunc.Rarity));
}
