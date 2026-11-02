using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// Every valuable that can be spawned, grouped by size.
///
/// The list covers every preset in the game, not only the ones the current level draws from:
/// an admin asking for a valuable has no reason to be limited to the level they happen to be
/// standing in.
///
/// Size is <c>ValuableVolume.Type</c>, and the game keeps one list per size on each
/// <c>LevelValuables</c> preset, so a valuable's size is known from the list it sits in and
/// needs no prefab load. Valuables another mod registered into no preset at all are added on
/// top, which does cost one prefab load each - that is the only way to read their size.
/// </summary>
internal static class ValuableCatalog
{
    private static readonly List<CatalogEntry<PrefabRef>> Entries = new List<CatalogEntry<PrefabRef>>();
    private static List<CatalogGroup<PrefabRef>> _groups = new List<CatalogGroup<PrefabRef>>();

    private static bool _reportedUnknownVolume;

    private static readonly CatalogState State = new CatalogState("valuables", Rebuild);

    internal static IReadOnlyList<CatalogEntry<PrefabRef>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<PrefabRef>> Groups
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
        _groups = new List<CatalogGroup<PrefabRef>>();

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> moddedNames = CollectModdedNames();

        AddFromPresets(moddedNames, seen);
        AddRegisteredRemainder(seen);

        _groups = CatalogGrouping.Build(Entries);
        Plugin.LogVerbose($"Valuable catalog: {Entries.Count} valuables in {_groups.Count} sizes.");
    }

    /// <summary>
    /// Walks every preset in the game rather than the ones this level happens to use, so a
    /// valuable from any level can be spawned in any other. REPOLib keeps that full list;
    /// without it the menu falls back to the current level, which is all the game itself
    /// exposes.
    /// </summary>
    private static IReadOnlyList<LevelValuables> AllPresets()
    {
        IReadOnlyList<LevelValuables> presets = RepoLibBridge.GetAllValuablePresets();
        if (presets.Count > 0)
        {
            return presets;
        }

        List<LevelValuables> ofThisLevel = RunManager.instance?.levelCurrent?.ValuablePresets;
        return ofThisLevel ?? (IReadOnlyList<LevelValuables>)new LevelValuables[0];
    }

    private static void AddFromPresets(HashSet<string> moddedNames, HashSet<string> seen)
    {
        foreach (LevelValuables preset in AllPresets())
        {
            if (preset == null)
            {
                continue;
            }

            foreach (ValuableVolume.Type volume in VolumeTypes)
            {
                List<PrefabRef> list = GetList(preset, volume);
                if (list == null)
                {
                    continue;
                }

                foreach (PrefabRef prefabRef in list)
                {
                    Add(prefabRef, volume, moddedNames, seen);
                }
            }
        }
    }

    /// <summary>
    /// Modded valuables the current level's presets never mention. Their size is only on the
    /// prefab, so each one is loaded once; a prefab that fails to load is skipped quietly
    /// because the game already logs that failure itself.
    /// </summary>
    private static void AddRegisteredRemainder(HashSet<string> seen)
    {
        foreach (PrefabRef prefabRef in RepoLibBridge.GetRegisteredValuables())
        {
            if (prefabRef == null || string.IsNullOrEmpty(prefabRef.PrefabName) || seen.Contains(prefabRef.PrefabName))
            {
                continue;
            }

            ValuableObject valuableObject = null;
            try
            {
                valuableObject = prefabRef.Prefab?.GetComponent<ValuableObject>();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not read the size of valuable \"{prefabRef.PrefabName}\". {ex.Message}");
            }

            if (valuableObject == null)
            {
                continue;
            }

            Add(prefabRef, valuableObject.volumeType, null, seen, forceModded: true);
        }
    }

    private static HashSet<string> CollectModdedNames()
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (PrefabRef prefabRef in RepoLibBridge.GetRegisteredValuables())
        {
            if (prefabRef != null && !string.IsNullOrEmpty(prefabRef.PrefabName))
            {
                names.Add(prefabRef.PrefabName);
            }
        }

        return names;
    }

    private static void Add(
        PrefabRef prefabRef,
        ValuableVolume.Type volume,
        HashSet<string> moddedNames,
        HashSet<string> seen,
        bool forceModded = false)
    {
        if (prefabRef == null || string.IsNullOrEmpty(prefabRef.PrefabName) || !seen.Add(prefabRef.PrefabName))
        {
            return;
        }

        bool modded = forceModded || (moddedNames != null && moddedNames.Contains(prefabRef.PrefabName));

        Entries.Add(new CatalogEntry<PrefabRef>(
            prefabRef.PrefabName,
            GameText.FromPrefabName(prefabRef.PrefabName),
            GameText.Humanize(volume.ToString()),
            (int)volume,
            modded ? ContentSource.Mod : ContentSource.Vanilla,
            prefabRef));
    }

    /// <summary>
    /// The preset's list for one size. Driven off the enum rather than off the seven field
    /// names, so a size the game adds later shows up as a warning instead of silently
    /// dropping its valuables.
    /// </summary>
    private static List<PrefabRef> GetList(LevelValuables preset, ValuableVolume.Type volume)
    {
        switch (volume)
        {
            case ValuableVolume.Type.Tiny: return preset.tiny;
            case ValuableVolume.Type.Small: return preset.small;
            case ValuableVolume.Type.Medium: return preset.medium;
            case ValuableVolume.Type.Big: return preset.big;
            case ValuableVolume.Type.Wide: return preset.wide;
            case ValuableVolume.Type.Tall: return preset.tall;
            case ValuableVolume.Type.VeryTall: return preset.veryTall;
            default:
                ReportUnknownVolume(volume);
                return null;
        }
    }

    private static void ReportUnknownVolume(ValuableVolume.Type volume)
    {
        if (_reportedUnknownVolume)
        {
            return;
        }

        _reportedUnknownVolume = true;
        Plugin.Log.LogWarning(
            $"ValuableVolume.Type.{volume} has no list on LevelValuables that this mod knows about, " +
            "so valuables of that size are missing from the menu.");
    }

    private static readonly ValuableVolume.Type[] VolumeTypes =
        (ValuableVolume.Type[])Enum.GetValues(typeof(ValuableVolume.Type));
}
