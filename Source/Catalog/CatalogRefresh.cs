namespace AdminMenu.Catalog;

/// <summary>
/// Decides when the catalogs are out of date.
///
/// The content a level offers is not the content of the previous one, and the game rebuilds
/// its own tables as it loads: <c>StatsManager.LoadItemsFromFolder</c> refills the item
/// dictionary from Resources, after which REPOLib restores the modded entries. So a catalog
/// built once at startup is wrong by the first level, and one rebuilt on every menu open
/// pays for prefab loads it does not need.
///
/// Instead of patching the game, the state the catalogs depend on is summarised in a token
/// that is cheap to read; the catalogs rebuild when it changes. Nothing to break on a game
/// update, and no work at all in a session where the menu is never opened.
/// </summary>
internal readonly struct CatalogRefresh
{
    private readonly Level _level;
    private readonly int _levelsCompleted;
    private readonly int _itemCount;
    private readonly int _upgradeCount;
    private readonly int _localeVersion;

    private CatalogRefresh(Level level, int levelsCompleted, int itemCount, int upgradeCount, int localeVersion)
    {
        _level = level;
        _levelsCompleted = levelsCompleted;
        _itemCount = itemCount;
        _upgradeCount = upgradeCount;
        _localeVersion = localeVersion;
    }

    /// <summary>The token for the state the game is in right now.</summary>
    internal static CatalogRefresh Current(int localeVersion)
    {
        RunManager runManager = RunManager.instance;
        StatsManager statsManager = StatsManager.instance;

        return new CatalogRefresh(
            runManager?.levelCurrent,
            runManager == null ? -1 : runManager.levelsCompleted,
            statsManager?.itemDictionary?.Count ?? -1,
            statsManager?.upgradesInfo?.Count ?? -1,
            localeVersion);
    }

    internal bool Matches(CatalogRefresh other)
    {
        return ReferenceEquals(_level, other._level) &&
               _levelsCompleted == other._levelsCompleted &&
               _itemCount == other._itemCount &&
               _upgradeCount == other._upgradeCount &&
               _localeVersion == other._localeVersion;
    }

    public override string ToString()
    {
        string levelName = _level == null ? "none" : _level.name;
        return $"level={levelName} completed={_levelsCompleted} items={_itemCount} upgrades={_upgradeCount} locale={_localeVersion}";
    }
}
