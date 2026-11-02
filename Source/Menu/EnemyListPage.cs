using System.Collections.Generic;
using AdminMenu.Catalog;
using AdminMenu.Features;
using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The enemy list, with how many of them to let loose.
///
/// The count is one control at the top of the page rather than one per row. Putting it on
/// every row meant placing two controls side by side on a line, which MenuLib does not do -
/// it gives each control a line of its own - and the hand-placed version came out crooked.
/// One count that applies to whatever is clicked next does the same job, and setting it is
/// one click rather than finding the right row first.
///
/// Clicking a name asks nothing: the spawn is announced on screen and counted down, which
/// leaves time to close the menu and be somewhere else when it arrives.
/// </summary>
internal static class EnemyListPage
{
    /// <summary>As many of one enemy as the page will offer at once.</summary>
    private const int MaxCount = 20;

    private static int _count = 1;

    internal static void Open()
    {
        EnemyCatalog.EnsureBuilt();
        MenuNavigator.Go(() => Build(null));
    }

    /// <summary>Forgets the chosen count. Called when a run ends.</summary>
    internal static void Reset()
    {
        _count = 1;
    }

    private static REPOPopupPage Build(string query)
    {
        REPOPopupPage page = MenuBuilder.CreatePage(
            MenuTitle.ForKey(StringKeys.TabSpawn, StringKeys.SpawnEnemies));

        MenuBuilder.IntSlider(
            page,
            Strings.Get(StringKeys.SpawnCount),
            min: 1,
            max: MaxCount,
            value: _count,
            onChanged: count => _count = count);

        MenuBuilder.InputField(page, Strings.Get(StringKeys.MenuSearch), query, submitted =>
            MenuNavigator.Replace(() => Build(submitted)));

        MenuBuilder.Spacer(page);

        IReadOnlyList<CatalogEntry<EnemySetup>> entries = string.IsNullOrWhiteSpace(query)
            ? EnemyCatalog.All
            : CatalogSearch.Filter(EnemyCatalog.All, query);

        foreach (CatalogEntry<EnemySetup> entry in entries)
        {
            CatalogEntry<EnemySetup> captured = entry;
            MenuBuilder.Button(page, Label(captured), () => SpawnCountdown.Begin(captured, _count));
        }

        return page;
    }

    private static string Label(CatalogEntry<EnemySetup> entry)
    {
        return entry.IsModded
            ? $"({Strings.Get(StringKeys.SourceMod)}) {entry.DisplayName}"
            : entry.DisplayName;
    }
}
