using System;
using AdminMenu.Localization;

namespace AdminMenu.Catalog;

/// <summary>
/// Keeps one catalog up to date, and no more often than it has to be.
///
/// Each catalog owns one of these. A catalog is rebuilt the first time it is read and then
/// only when <see cref="CatalogRefresh"/> says the game moved on - a new level, a reloaded
/// item table, a language change. Building is per catalog rather than all at once because
/// reading enemy names costs a prefab load each: doing that work when the menu opens, for a
/// list the player may never look at, is what made the menu take seconds to appear.
/// </summary>
internal sealed class CatalogState
{
    private readonly string _what;
    private readonly Action _rebuild;

    private CatalogRefresh _builtFor;
    private bool _built;

    internal CatalogState(string what, Action rebuild)
    {
        _what = what;
        _rebuild = rebuild;
    }

    /// <summary>Rebuilds if anything the catalog depends on changed. True if it did.</summary>
    internal bool EnsureBuilt()
    {
        LocaleWatcher.EnsureSubscribed();

        CatalogRefresh current = CatalogRefresh.Current(LocaleWatcher.Version);
        if (_built && _builtFor.Matches(current))
        {
            return false;
        }

        try
        {
            _rebuild();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not build the {_what} catalog, so that list will be empty. {ex}");
        }

        // Marked built either way: a catalog that throws would otherwise try again on every
        // read and fill the log with the same failure.
        _builtFor = current;
        _built = true;
        return true;
    }

    /// <summary>Forces the next read to rebuild.</summary>
    internal void Invalidate()
    {
        _built = false;
    }
}
