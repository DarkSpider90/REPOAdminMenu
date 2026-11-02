using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AdminMenu.Interop;

/// <summary>
/// Reaches R.E.P.O.'s registry of stat dictionaries.
///
/// <c>StatsManager.dictionaryOfDictionaries</c> maps a dictionary name to a
/// <c>steamID -&gt; value</c> table. Everything in that registry is synchronised between
/// players by <c>PunManager.SyncAllDictionaries</c> and written to and read back from the
/// save file by <c>StatsManager.SaveGame</c> and <c>LoadGame</c>, which is why upgrade
/// levels are read from and written to it rather than to a table of this mod's own. The
/// field is internal to Assembly-CSharp, so it is reached by reflection; every accessor
/// degrades to a no-op if a game update renames it.
/// </summary>
internal static class StatsDictionaryAccess
{
    private static readonly FieldInfo RegistryField =
        AccessTools.Field(typeof(StatsManager), "dictionaryOfDictionaries");

    private static bool _reportedMissingRegistry;

    internal static bool IsAvailable => RegistryField != null;

    internal static IDictionary<string, Dictionary<string, int>> GetRegistry(StatsManager statsManager)
    {
        if (statsManager == null)
        {
            return null;
        }

        if (RegistryField == null)
        {
            ReportMissingRegistry();
            return null;
        }

        try
        {
            return RegistryField.GetValue(statsManager) as IDictionary<string, Dictionary<string, int>>;
        }
        catch (Exception ex)
        {
            ReportMissingRegistry(ex);
            return null;
        }
    }

    internal static bool TryGetTable(string dictionaryName, out Dictionary<string, int> table)
    {
        table = null;

        IDictionary<string, Dictionary<string, int>> registry = GetRegistry(StatsManager.instance);
        return registry != null && registry.TryGetValue(dictionaryName, out table) && table != null;
    }

    internal static int GetValue(string dictionaryName, string steamId, int fallback = 0)
    {
        if (string.IsNullOrEmpty(steamId) || !TryGetTable(dictionaryName, out Dictionary<string, int> table))
        {
            return fallback;
        }

        return table.TryGetValue(steamId, out int value) ? value : fallback;
    }

    internal static bool ContainsTable(string dictionaryName)
    {
        return TryGetTable(dictionaryName, out _);
    }

    /// <summary>
    /// Writes through the game's own setter, which is the only write the save file and
    /// <c>SyncAllDictionaries</c> both pick up.
    /// </summary>
    internal static void SetValue(string dictionaryName, string steamId, int value)
    {
        if (StatsManager.instance == null || string.IsNullOrEmpty(steamId))
        {
            return;
        }

        StatsManager.instance.DictionaryUpdateValue(dictionaryName, steamId, value);
    }

    /// <summary>Adds an empty table under this name if the registry has none yet.</summary>
    internal static bool EnsureTable(string dictionaryName)
    {
        IDictionary<string, Dictionary<string, int>> registry = GetRegistry(StatsManager.instance);
        if (registry == null)
        {
            return false;
        }

        if (!registry.ContainsKey(dictionaryName))
        {
            registry[dictionaryName] = new Dictionary<string, int>();
        }

        return true;
    }

    private static void ReportMissingRegistry(Exception ex = null)
    {
        if (_reportedMissingRegistry)
        {
            return;
        }

        _reportedMissingRegistry = true;
        Plugin.Log.LogWarning(
            "StatsManager.dictionaryOfDictionaries could not be reached, so upgrade levels cannot be read " +
            "or written. " + (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
