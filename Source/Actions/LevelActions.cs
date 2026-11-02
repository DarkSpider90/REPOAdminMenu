using AdminMenu.Access;
using AdminMenu.Catalog;
using UnityEngine;

namespace AdminMenu.Actions;

/// <summary>
/// Sends the run to a chosen level and sets how far along it is.
///
/// The game picks the next level itself, at random, in <c>RunManager.SetRunLevel</c>. There is
/// no call that says "go to this one", so a choice is parked here and
/// <see cref="Patches.RunManagerPatches"/> hands it over the next time the game asks for a
/// level. That is one small patch on one method, and if the patch ever fails the game simply
/// keeps choosing at random.
///
/// The level number goes through the same two calls the game's own <c>/levelnum</c> command
/// uses, so the number on the truck screen and the saved run stat stay in step.
/// </summary>
internal static class LevelActions
{
    private static Level _pendingLevel;

    /// <summary>The level the game will be given instead of a random one, if any.</summary>
    internal static Level PendingLevel => _pendingLevel;

    internal static Level CurrentLevel => RunManager.instance?.levelCurrent;

    internal static int LevelsCompleted => RunManager.instance == null ? 0 : RunManager.instance.levelsCompleted;

    /// <summary>
    /// Takes the parked choice, clearing it. Called from the patch, which is the only place
    /// that should consume it.
    /// </summary>
    internal static Level ConsumePendingLevel()
    {
        Level level = _pendingLevel;
        _pendingLevel = null;
        return level;
    }

    internal static void ClearPendingLevel()
    {
        _pendingLevel = null;
    }

    /// <summary>
    /// Goes to this level now. The level is parked for the patch and then the run is told to
    /// change, which is what makes the game ask for a level in the first place.
    /// </summary>
    internal static bool ChangeTo(CatalogEntry<Level> entry)
    {
        if (entry?.Payload == null || !Allowed())
        {
            return false;
        }

        RunManager runManager = RunManager.instance;
        if (runManager == null)
        {
            return false;
        }

        _pendingLevel = entry.Payload;

        runManager.ChangeLevel(_completedLevel: false, _levelFailed: false, RunManager.ChangeLevelType.RunLevel);
        Plugin.LogVerbose($"Changing level to \"{entry.DisplayName}\".");
        return true;
    }

    /// <summary>Restarts the level the run is on, keeping the level number.</summary>
    internal static bool Restart()
    {
        if (!Allowed() || RunManager.instance == null)
        {
            return false;
        }

        _pendingLevel = RunManager.instance.levelCurrent;
        RunManager.instance.ChangeLevel(_completedLevel: false, _levelFailed: false, RunManager.ChangeLevelType.RunLevel);
        return true;
    }

    /// <summary>
    /// Sets how many levels the run has completed. This drives enemy difficulty and the
    /// layout size, so it is the number the menu's arrows and text box change.
    /// </summary>
    internal static bool SetLevelsCompleted(int completed)
    {
        if (!Allowed() || RunManager.instance == null)
        {
            return false;
        }

        completed = Mathf.Max(completed, 0);
        RunManager.instance.levelsCompleted = completed;
        SemiFunc.StatSetRunLevel(completed);

        Plugin.LogVerbose($"Levels completed set to {completed}.");
        return true;
    }

    internal static bool AddLevelsCompleted(int amount)
    {
        return SetLevelsCompleted(LevelsCompleted + amount);
    }

    private static bool Allowed()
    {
        if (AdminAccess.CanAct)
        {
            return true;
        }

        Plugin.Log.LogWarning("Only the host can change the level.");
        return false;
    }
}
