using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using UnityEngine;

namespace AdminMenu.Interop;

/// <summary>
/// The single place that touches REPOLib.
///
/// REPOLib is a soft dependency: it is what puts modded items, valuables, enemies and
/// upgrades into the menu on equal footing with vanilla ones, and without it the menu still
/// works on vanilla content alone. That is why every REPOLib type stays inside a method body
/// here and none appears in a field or a signature - the runtime resolves a method's types
/// when the method is first called, so a missing REPOLib costs nothing as long as nothing
/// calls past <see cref="Available"/>.
/// </summary>
internal static class RepoLibBridge
{
    private const string RepoLibGuid = "REPOLib";

    private static readonly Item[] NoItems = new Item[0];
    private static readonly PrefabRef[] NoValuables = new PrefabRef[0];
    private static readonly EnemySetup[] NoEnemies = new EnemySetup[0];
    private static readonly string[] NoIds = new string[0];
    private static readonly Level[] NoLevels = new Level[0];
    private static readonly LevelValuables[] NoPresets = new LevelValuables[0];

    private static bool? _available;

    internal static bool Available
    {
        get
        {
            _available ??= Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(RepoLibGuid);
            return _available.Value;
        }
    }

    /// <summary>Items other mods registered. The game's own dictionary drops them on reload.</summary>
    internal static IReadOnlyList<Item> GetRegisteredItems()
    {
        if (!Available)
        {
            return NoItems;
        }

        try
        {
            return REPOLib.Modules.Items.RegisteredItems ?? (IReadOnlyList<Item>)NoItems;
        }
        catch (Exception ex)
        {
            ReportFailure("registered items", ex);
            return NoItems;
        }
    }

    internal static IReadOnlyList<PrefabRef> GetRegisteredValuables()
    {
        if (!Available)
        {
            return NoValuables;
        }

        try
        {
            return REPOLib.Modules.Valuables.RegisteredValuables ?? (IReadOnlyList<PrefabRef>)NoValuables;
        }
        catch (Exception ex)
        {
            ReportFailure("registered valuables", ex);
            return NoValuables;
        }
    }

    internal static IReadOnlyList<EnemySetup> GetRegisteredEnemies()
    {
        if (!Available)
        {
            return NoEnemies;
        }

        try
        {
            return REPOLib.Modules.Enemies.RegisteredEnemies ?? (IReadOnlyList<EnemySetup>)NoEnemies;
        }
        catch (Exception ex)
        {
            ReportFailure("registered enemies", ex);
            return NoEnemies;
        }
    }

    /// <summary>
    /// Spawns through REPOLib, which raises the network event that gives every client the
    /// same object. Returns false when REPOLib is absent, so the caller can fall back to a
    /// plain local instantiate.
    /// </summary>
    internal static bool TrySpawnItem(Item item, Vector3 position, Quaternion rotation, out GameObject spawned)
    {
        spawned = null;
        if (!Available || item == null)
        {
            return false;
        }

        try
        {
            spawned = REPOLib.Modules.Items.SpawnItem(item, position, rotation);
            return spawned != null;
        }
        catch (Exception ex)
        {
            ReportFailure($"spawn of item {Quote(item.itemName)}", ex);
            return false;
        }
    }

    internal static bool TrySpawnValuable(PrefabRef prefabRef, Vector3 position, Quaternion rotation, out GameObject spawned)
    {
        spawned = null;
        if (!Available || prefabRef == null)
        {
            return false;
        }

        try
        {
            spawned = REPOLib.Modules.Valuables.SpawnValuable(prefabRef, position, rotation);
            return spawned != null;
        }
        catch (Exception ex)
        {
            ReportFailure($"spawn of valuable {Quote(prefabRef.PrefabName)}", ex);
            return false;
        }
    }

    /// <summary>
    /// Spawns an enemy and says how many creatures that turned out to be. One setup can carry
    /// several - a group of gnomes is one entry in the list - and the count is reported so a
    /// "why did a crowd appear" can be answered from the log instead of guessed at.
    /// </summary>
    internal static bool TrySpawnEnemy(EnemySetup enemySetup, Vector3 position, Quaternion rotation, out int spawned)
    {
        spawned = 0;
        if (!Available || enemySetup == null)
        {
            return false;
        }

        try
        {
            List<EnemyParent> parents =
                REPOLib.Modules.Enemies.SpawnEnemy(enemySetup, position, rotation, spawnDespawned: false);

            spawned = parents?.Count ?? 0;
            if (parents != null)
            {
                foreach (EnemyParent parent in parents)
                {
                    // Each one is about to despawn itself and wait seconds before spawning
                    // for real; this cuts the wait to a frame.
                    Features.SpawnedEnemyGuard.Hurry(parent);
                }
            }

            return spawned > 0;
        }
        catch (Exception ex)
        {
            ReportFailure($"spawn of enemy {Quote(enemySetup.name)}", ex);
            return false;
        }
    }

    /// <summary>
    /// Every valuable preset in the game, not just the ones the current level uses. This is
    /// what lets the menu offer a valuable from any level instead of only what happens to be
    /// in this one.
    /// </summary>
    internal static IReadOnlyList<LevelValuables> GetAllValuablePresets()
    {
        if (!Available)
        {
            return NoPresets;
        }

        try
        {
            IReadOnlyDictionary<string, LevelValuables> presets =
                REPOLib.Modules.ValuablePresets.AllValuablePresets;
            if (presets == null)
            {
                return NoPresets;
            }

            List<LevelValuables> all = new List<LevelValuables>(presets.Count);
            foreach (KeyValuePair<string, LevelValuables> pair in presets)
            {
                if (pair.Value != null)
                {
                    all.Add(pair.Value);
                }
            }

            return all;
        }
        catch (Exception ex)
        {
            ReportFailure("valuable presets", ex);
            return NoPresets;
        }
    }

    internal static IReadOnlyList<Level> GetRegisteredLevels()
    {
        if (!Available)
        {
            return NoLevels;
        }

        try
        {
            return REPOLib.Modules.Levels.RegisteredLevels ?? (IReadOnlyList<Level>)NoLevels;
        }
        catch (Exception ex)
        {
            ReportFailure("registered levels", ex);
            return NoLevels;
        }
    }

    /// <summary>
    /// Spawns any prefab the game already knows as a network prefab. Used for the cosmetic
    /// boxes, which are not items or valuables and so have no module of their own, but whose
    /// setup carries a plain <c>PrefabRef</c>.
    /// </summary>
    internal static bool TrySpawnNetworkPrefab(PrefabRef prefabRef, Vector3 position, Quaternion rotation, out GameObject spawned)
    {
        spawned = null;
        if (!Available || prefabRef == null)
        {
            return false;
        }

        try
        {
            spawned = REPOLib.Modules.NetworkPrefabs.SpawnNetworkPrefab(prefabRef, position, rotation);
            return spawned != null;
        }
        catch (Exception ex)
        {
            ReportFailure($"spawn of prefab {Quote(prefabRef.PrefabName)}", ex);
            return false;
        }
    }

    /// <summary>Upgrade ids registered by other mods, in registration order.</summary>
    internal static IReadOnlyList<string> GetModdedUpgradeIds()
    {
        if (!Available)
        {
            return NoIds;
        }

        try
        {
            IReadOnlyList<REPOLib.Modules.PlayerUpgrade> upgrades = REPOLib.Modules.Upgrades.PlayerUpgrades;
            if (upgrades == null)
            {
                return NoIds;
            }

            List<string> ids = new List<string>(upgrades.Count);
            foreach (REPOLib.Modules.PlayerUpgrade upgrade in upgrades)
            {
                if (upgrade != null && !string.IsNullOrEmpty(upgrade.UpgradeId))
                {
                    ids.Add(upgrade.UpgradeId);
                }
            }

            return ids;
        }
        catch (Exception ex)
        {
            ReportFailure("registered upgrades", ex);
            return NoIds;
        }
    }

    /// <summary>
    /// Name of the shop item the upgrade was registered with, used as a display name when the
    /// mod wrote nothing into <c>StatsManager.upgradesInfo</c>. Empty when there is no item.
    /// </summary>
    internal static string GetModdedUpgradeItemName(string upgradeId)
    {
        if (!Available || string.IsNullOrEmpty(upgradeId))
        {
            return string.Empty;
        }

        try
        {
            return REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out REPOLib.Modules.PlayerUpgrade upgrade) &&
                   upgrade.Item != null
                ? upgrade.Item.itemName ?? string.Empty
                : string.Empty;
        }
        catch (Exception ex)
        {
            ReportFailure($"item name of upgrade {Quote(upgradeId)}", ex);
            return string.Empty;
        }
    }

    internal static bool IsModdedUpgrade(string upgradeId)
    {
        if (!Available || string.IsNullOrEmpty(upgradeId))
        {
            return false;
        }

        try
        {
            return REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out _);
        }
        catch (Exception ex)
        {
            ReportFailure($"lookup of upgrade {Quote(upgradeId)}", ex);
            return false;
        }
    }

    /// <summary>
    /// Sets a modded upgrade to an absolute level. REPOLib applies the mod's own upgrade
    /// action and raises its networked event, which is why modded upgrades do not go through
    /// the game's upgrade RPC: that RPC's fall-through only writes the dictionary and would
    /// never run the mod's code.
    /// </summary>
    internal static bool TrySetModdedUpgradeLevel(string upgradeId, string steamId, int level)
    {
        if (!Available || string.IsNullOrEmpty(upgradeId) || string.IsNullOrEmpty(steamId))
        {
            return false;
        }

        try
        {
            if (!REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out REPOLib.Modules.PlayerUpgrade upgrade))
            {
                return false;
            }

            upgrade.SetLevel(steamId, Mathf.Max(level, 0));
            return true;
        }
        catch (Exception ex)
        {
            ReportFailure($"level change of upgrade {Quote(upgradeId)}", ex);
            return false;
        }
    }

    internal static bool TryGetModdedUpgradeLevel(string upgradeId, string steamId, out int level)
    {
        level = 0;
        if (!Available || string.IsNullOrEmpty(upgradeId) || string.IsNullOrEmpty(steamId))
        {
            return false;
        }

        try
        {
            if (!REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out REPOLib.Modules.PlayerUpgrade upgrade))
            {
                return false;
            }

            level = upgrade.GetLevel(steamId);
            return true;
        }
        catch (Exception ex)
        {
            ReportFailure($"level of upgrade {Quote(upgradeId)}", ex);
            return false;
        }
    }

    /// <summary>
    /// The table REPOLib keeps the upgrade's levels in. Handed out in plain types so the
    /// repair in <c>UpgradeDictionaryLink</c> needs no REPOLib type of its own.
    /// </summary>
    internal static bool TryGetModdedUpgradeTable(string upgradeId, out Dictionary<string, int> table)
    {
        table = null;
        if (!Available || string.IsNullOrEmpty(upgradeId))
        {
            return false;
        }

        try
        {
            if (!REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out REPOLib.Modules.PlayerUpgrade upgrade))
            {
                return false;
            }

            table = upgrade.PlayerDictionary;
            return table != null;
        }
        catch (Exception ex)
        {
            ReportFailure($"table of upgrade {Quote(upgradeId)}", ex);
            return false;
        }
    }

    internal static bool TrySetModdedUpgradeTable(string upgradeId, Dictionary<string, int> table)
    {
        if (!Available || string.IsNullOrEmpty(upgradeId) || table == null)
        {
            return false;
        }

        try
        {
            if (!REPOLib.Modules.Upgrades.TryGetUpgrade(upgradeId, out REPOLib.Modules.PlayerUpgrade upgrade))
            {
                return false;
            }

            upgrade.PlayerDictionary = table;
            return true;
        }
        catch (Exception ex)
        {
            ReportFailure($"table of upgrade {Quote(upgradeId)}", ex);
            return false;
        }
    }

    private static string Quote(string value)
    {
        return "\"" + (value ?? string.Empty) + "\"";
    }

    private static void ReportFailure(string what, Exception ex)
    {
        Plugin.Log.LogError($"REPOLib refused the {what}. {ex}");
    }
}
