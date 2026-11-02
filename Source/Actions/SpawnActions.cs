using AdminMenu.Access;
using AdminMenu.Catalog;
using AdminMenu.Interop;
using AdminMenu.Targeting;
using UnityEngine;

namespace AdminMenu.Actions;

/// <summary>
/// Puts something in the world next to the target player.
///
/// Everything goes through REPOLib, which spawns a network prefab the whole lobby receives and
/// refuses the call when the caller is not the host. That also replaces the reference mod's
/// hand-rolled enemy spawn, which had to switch off the enemy despawner while it worked:
/// REPOLib 4.3.0 brackets the spawn with the game's own
/// <c>EnemiesSpawnedRemoveStart</c>/<c>End</c>, so no patch is needed to keep a fresh enemy
/// from being cleaned up.
///
/// Without REPOLib there is no spawning at all, and the menu says so rather than quietly
/// instantiating something only the host can see.
/// </summary>
internal static class SpawnActions
{
    /// <summary>Battery charge a freshly spawned item starts with, as a percentage.</summary>
    private const int FullBattery = 100;

    /// <summary>How far apart repeats of the same enemy are placed.</summary>
    private const float ScatterRadius = 1.2f;

    private static bool _reportedMissingRepoLib;

    internal static bool CanSpawn => AdminAccess.CanAct && RepoLibBridge.Available;

    internal static bool SpawnItem(CatalogEntry<Item> entry)
    {
        if (entry == null || !Allowed() || !TryGetPoint(out Vector3 position))
        {
            return false;
        }

        if (!RepoLibBridge.TrySpawnItem(entry.Payload, position, Quaternion.identity, out GameObject spawned))
        {
            return false;
        }

        // A tool spawned at zero charge is useless, so it comes out full.
        ItemBattery battery = spawned.GetComponent<ItemBattery>() ?? spawned.GetComponentInChildren<ItemBattery>(true);
        if (battery != null)
        {
            battery.SetBatteryLife(FullBattery);
        }

        Done(entry.DisplayName, position);
        return true;
    }

    internal static bool SpawnValuable(CatalogEntry<PrefabRef> entry)
    {
        if (entry == null || !Allowed() || !TryGetPoint(out Vector3 position))
        {
            return false;
        }

        if (!RepoLibBridge.TrySpawnValuable(entry.Payload, position, Quaternion.identity, out GameObject _))
        {
            return false;
        }

        Done(entry.DisplayName, position);
        return true;
    }

    /// <summary>
    /// Spawns an enemy, or several of it. Each one is placed separately so a pack does not
    /// arrive inside itself; one failure does not stop the rest.
    /// </summary>
    internal static bool SpawnEnemy(CatalogEntry<EnemySetup> entry, int times = 1)
    {
        if (entry == null || times <= 0 || !Allowed())
        {
            return false;
        }

        int spawned = 0;

        for (int i = 0; i < times; i++)
        {
            if (!TryGetPoint(out Vector3 position))
            {
                break;
            }

            if (RepoLibBridge.TrySpawnEnemy(entry.Payload, Scatter(position, i), Quaternion.identity, out int made))
            {
                spawned += made;
            }
        }

        if (spawned == 0)
        {
            return false;
        }

        // Always logged, not only when diagnostics are on: an enemy that arrives in numbers
        // is the kind of thing worth being able to count after the fact.
        Plugin.Log.LogInfo($"Spawned {spawned} x \"{entry.DisplayName}\".");
        return true;
    }

    /// <summary>
    /// Spreads repeats around the target in a ring, so three of something do not land in one
    /// place and shove each other across the room.
    /// </summary>
    private static Vector3 Scatter(Vector3 origin, int index)
    {
        if (index == 0)
        {
            return origin;
        }

        float angle = index * 137.5f * Mathf.Deg2Rad;
        return origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ScatterRadius;
    }

    internal static bool SpawnCosmeticBox(CatalogEntry<ValuableDirector.CosmeticWorldObjectSetup> entry)
    {
        if (entry?.Payload?.prefab == null || !Allowed() || !TryGetPoint(out Vector3 position))
        {
            return false;
        }

        if (!RepoLibBridge.TrySpawnNetworkPrefab(entry.Payload.prefab, position, Quaternion.identity, out GameObject _))
        {
            return false;
        }

        Done(entry.DisplayName, position);
        return true;
    }

    /// <summary>Where the next spawn lands: in front of the target player.</summary>
    private static bool TryGetPoint(out Vector3 position)
    {
        if (TargetPlayerService.TryGetSpawnPoint(out position))
        {
            return true;
        }

        Plugin.Log.LogWarning("There is nowhere to spawn: no target player and no camera to fall back on.");
        return false;
    }

    private static bool Allowed()
    {
        if (!AdminAccess.CanAct)
        {
            Plugin.Log.LogWarning("Only the host can spawn things.");
            return false;
        }

        if (RepoLibBridge.Available)
        {
            return true;
        }

        if (!_reportedMissingRepoLib)
        {
            _reportedMissingRepoLib = true;
            Plugin.Log.LogWarning(
                "REPOLib is not installed, so nothing can be spawned - a spawn has to reach every " +
                "player, and REPOLib is what does that.");
        }

        return false;
    }

    private static void Done(string what, Vector3 position)
    {
        Plugin.LogVerbose($"Spawned \"{what}\" at {position} for {TargetPlayerService.Current}.");
    }
}
