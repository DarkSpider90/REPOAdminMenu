using System;
using System.Collections.Generic;
using AdminMenu.Interop;
using AdminMenu.Localization;
using UnityEngine;

namespace AdminMenu.Catalog;

/// <summary>
/// Every enemy that can be spawned, as one flat list.
///
/// The game files enemies under three difficulty tiers, which say something about where the
/// game will place them on its own and nothing at all to someone picking one from a menu, so
/// the tiers are not shown. There are few enough enemies to read the list at once.
///
/// An <c>EnemySetup</c> carries no name of its own; the readable one sits on the
/// <c>EnemyParent</c> of its first spawn object, which costs one prefab load to reach. Those
/// names are resolved once per rebuild and kept for the rest of it. This is why there is no
/// list of enemy names anywhere in this mod: one would go out of date the day the game adds
/// an enemy, and sooner than that for anyone running a content mod.
/// </summary>
internal static class EnemyCatalog
{
    private static readonly List<CatalogEntry<EnemySetup>> Entries = new List<CatalogEntry<EnemySetup>>();
    private static readonly Dictionary<string, string> NameCache = new Dictionary<string, string>(StringComparer.Ordinal);

    private static List<CatalogGroup<EnemySetup>> _groups = new List<CatalogGroup<EnemySetup>>();

    private static readonly CatalogState State = new CatalogState("enemies", Rebuild);

    internal static IReadOnlyList<CatalogEntry<EnemySetup>> All
    {
        get
        {
            State.EnsureBuilt();
            return Entries;
        }
    }

    internal static IReadOnlyList<CatalogGroup<EnemySetup>> Groups
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
        // Dropped rather than kept: a rebuild only happens when the level or the language
        // changed, and after a language change the old names are the wrong ones.
        NameCache.Clear();

        Entries.Clear();
        _groups = new List<CatalogGroup<EnemySetup>>();

        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> moddedNames = CollectModdedNames();

        EnemyDirector director = EnemyDirector.instance;
        if (director != null)
        {
            AddTier(director.enemiesDifficulty1, 1, moddedNames, seen);
            AddTier(director.enemiesDifficulty2, 2, moddedNames, seen);
            AddTier(director.enemiesDifficulty3, 3, moddedNames, seen);
        }

        // Enemies REPOLib registered that the director has not taken on yet.
        AddTier(RepoLibBridge.GetRegisteredEnemies(), 1, moddedNames, seen);

        KeepSmallestOfEachName();

        Entries.Sort((left, right) =>
            string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));

        _groups = CatalogGrouping.Build(Entries);
        Plugin.LogVerbose($"Enemy catalog: {Entries.Count} enemies.");
    }

    /// <summary>
    /// Drops the pack versions of an enemy that also exists on its own.
    ///
    /// The game ships more than one setup under the same name: one that places a single
    /// creature and one that places three of them at once. Both come out of the director as
    /// separate entries, so the list showed every such enemy twice and picking the wrong one
    /// dropped a pack on the level. Where a name has a single-creature setup, that is the one
    /// kept; where it only ever appears as a pack, the pack stays, because otherwise that
    /// enemy could not be spawned at all.
    /// </summary>
    private static void KeepSmallestOfEachName()
    {
        Dictionary<string, CatalogEntry<EnemySetup>> smallest =
            new Dictionary<string, CatalogEntry<EnemySetup>>(StringComparer.CurrentCultureIgnoreCase);

        foreach (CatalogEntry<EnemySetup> entry in Entries)
        {
            if (!smallest.TryGetValue(entry.DisplayName, out CatalogEntry<EnemySetup> kept) ||
                CreatureCount(entry) < CreatureCount(kept))
            {
                smallest[entry.DisplayName] = entry;
            }
        }

        if (smallest.Count == Entries.Count)
        {
            return;
        }

        Plugin.LogVerbose($"Enemy catalog: dropped {Entries.Count - smallest.Count} pack duplicates.");

        Entries.Clear();
        Entries.AddRange(smallest.Values);
    }

    private static int CreatureCount(CatalogEntry<EnemySetup> entry)
    {
        return entry.Payload?.spawnObjects?.Count ?? 1;
    }

    private static HashSet<string> CollectModdedNames()
    {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (EnemySetup setup in RepoLibBridge.GetRegisteredEnemies())
        {
            if (setup != null && !string.IsNullOrEmpty(setup.name))
            {
                names.Add(setup.name);
            }
        }

        return names;
    }

    private static void AddTier(
        IReadOnlyList<EnemySetup> setups,
        int difficulty,
        HashSet<string> moddedNames,
        HashSet<string> seen)
    {
        if (setups == null)
        {
            return;
        }

        foreach (EnemySetup setup in setups)
        {
            if (setup == null || string.IsNullOrEmpty(setup.name) || !seen.Add(setup.name))
            {
                continue;
            }

            Entries.Add(new CatalogEntry<EnemySetup>(
                setup.name,
                ResolveName(setup),
                $"Difficulty {difficulty}",
                difficulty,
                moddedNames.Contains(setup.name) ? ContentSource.Mod : ContentSource.Vanilla,
                setup));
        }
    }

    private static string ResolveName(EnemySetup setup)
    {
        if (NameCache.TryGetValue(setup.name, out string cached))
        {
            return cached;
        }

        string resolved = ReadNameFromPrefab(setup);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            resolved = GameText.FromPrefabName(setup.name);
        }

        NameCache[setup.name] = resolved;
        return resolved;
    }

    private static string ReadNameFromPrefab(EnemySetup setup)
    {
        if (setup.spawnObjects == null)
        {
            return string.Empty;
        }

        foreach (PrefabRef prefabRef in setup.spawnObjects)
        {
            if (prefabRef == null)
            {
                continue;
            }

            try
            {
                GameObject prefab = prefabRef.Prefab;
                EnemyParent parent = prefab?.GetComponent<EnemyParent>() ?? prefab?.GetComponentInChildren<EnemyParent>(true);
                if (parent == null)
                {
                    continue;
                }

                return GameText.Resolve(parent.enemyNameLocalized, parent.enemyName);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not read the name of enemy \"{setup.name}\". {ex.Message}");
                return string.Empty;
            }
        }

        return string.Empty;
    }
}
