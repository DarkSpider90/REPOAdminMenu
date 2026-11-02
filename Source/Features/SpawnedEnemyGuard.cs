using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AdminMenu.Features;

/// <summary>
/// Gets an enemy the menu just spawned onto its feet without the two-to-five second wait.
///
/// Every enemy the game places runs the same opening move: once its setup finishes, its own
/// logic despawns it and waits two to five seconds before spawning it for real. For an enemy
/// the level placed that is invisible. For one dropped in front of the host it reads as a bug -
/// the thing appears, blinks out, and comes back seconds later.
///
/// The first attempt at this skipped the despawn outright, and that was worse in a way nobody
/// could see from the host's chair. Despawning and spawning are how the host tells everyone
/// else what an enemy is doing: <c>Spawn</c> sends <c>SpawnRPC</c> to every client, and that is
/// the signal other mods listen for to put the enemy on the map. Skipping it left clients with
/// an enemy that was never announced - present in the world, absent from their map.
///
/// So the wait is shortened instead of skipped. The timer the enemy is counting down is held
/// at zero until it spawns, which takes one frame rather than seconds, and every message the
/// game would normally send still gets sent in the normal order.
/// </summary>
internal static class SpawnedEnemyGuard
{
    /// <summary>
    /// How long to keep pushing. The enemy gets there in a frame or two; the rest is slack for
    /// a slow setup, after which it is left to its own devices rather than being held awake
    /// for the rest of the run.
    /// </summary>
    private const float Window = 5f;

    private static readonly FieldInfo SpawnedField = AccessTools.Field(typeof(EnemyParent), "Spawned");

    private static readonly List<Entry> Waiting = new List<Entry>();

    private static bool _reportedMissingField;

    /// <summary>Marks an enemy as just spawned by the menu.</summary>
    internal static void Hurry(EnemyParent parent)
    {
        if (parent != null)
        {
            Waiting.Add(new Entry(parent, Time.unscaledTime + Window));
        }
    }

    /// <summary>
    /// Holds the despawn countdown at zero for everything still waiting. Called once a frame;
    /// does nothing at all when nothing is waiting, which is almost always.
    /// </summary>
    internal static void Tick()
    {
        if (Waiting.Count == 0)
        {
            return;
        }

        float now = Time.unscaledTime;

        for (int i = Waiting.Count - 1; i >= 0; i--)
        {
            Entry entry = Waiting[i];

            if (entry.Parent == null || now >= entry.Deadline || IsSpawned(entry.Parent))
            {
                Waiting.RemoveAt(i);
                continue;
            }

            // The enemy's own logic spawns it as soon as this reaches zero, and sends the
            // spawn to every client while it does.
            entry.Parent.DespawnedTimer = 0f;
        }
    }

    internal static void Clear()
    {
        Waiting.Clear();
    }

    private static bool IsSpawned(EnemyParent parent)
    {
        if (SpawnedField == null)
        {
            Report();

            // Without the flag there is no way to tell when to stop, so the deadline is left
            // to do it.
            return false;
        }

        try
        {
            return SpawnedField.GetValue(parent) is bool spawned && spawned;
        }
        catch (Exception ex)
        {
            Report(ex);
            return false;
        }
    }

    private static void Report(Exception ex = null)
    {
        if (_reportedMissingField)
        {
            return;
        }

        _reportedMissingField = true;
        Plugin.Log.LogWarning(
            "EnemyParent.Spawned could not be read, so a spawned enemy is hurried along for a fixed " +
            "few seconds instead. " + (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }

    private readonly struct Entry
    {
        internal Entry(EnemyParent parent, float deadline)
        {
            Parent = parent;
            Deadline = deadline;
        }

        internal EnemyParent Parent { get; }

        internal float Deadline { get; }
    }
}
