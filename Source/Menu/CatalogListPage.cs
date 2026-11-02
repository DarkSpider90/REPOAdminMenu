using System;
using System.Collections.Generic;
using AdminMenu.Catalog;
using AdminMenu.Configuration;
using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// One page for every content list: items, valuables, enemies, cosmetic boxes, levels.
///
/// It takes a catalog and something to do with an entry, so all five lists share this page
/// instead of each getting one of its own. Written out by hand, the valuable sizes alone would
/// be seven near-identical pages, and an eighth the day the game adds a size.
///
/// A list is either grouped or flat. Items and valuables are worth splitting by the game's own
/// categories; enemies are not, because there are few enough of them to read at once and the
/// game's difficulty tiers say nothing a player picking an enemy cares about.
///
/// Search cuts across the categories either way, because with a few content mods installed
/// the item list runs past a hundred entries.
/// </summary>
internal static class CatalogListPage
{
    /// <summary>Opens the list as a new step, which is how it is reached from a menu button.</summary>
    internal static void Open<TPayload>(
        string title,
        IReadOnlyList<CatalogGroup<TPayload>> groups,
        IReadOnlyList<CatalogEntry<TPayload>> all,
        Func<CatalogEntry<TPayload>, bool> onPick)
    {
        MenuNavigator.Go(() => Build(title, groups, all, onPick, null));
    }

    private static REPOPopupPage Build<TPayload>(
        string title,
        IReadOnlyList<CatalogGroup<TPayload>> groups,
        IReadOnlyList<CatalogEntry<TPayload>> all,
        Func<CatalogEntry<TPayload>, bool> onPick,
        string query)
    {
        REPOPopupPage page = MenuBuilder.CreatePage(title);

        // Reporting only on Enter: the page is drawn again from the new text, and reporting
        // every keystroke would redraw it once per letter. The redraw replaces this page
        // rather than stacking, so Back leaves the list instead of undoing the search.
        MenuBuilder.InputField(page, Strings.Get(StringKeys.MenuSearch), query, submitted =>
            MenuNavigator.Replace(() => Build(title, groups, all, onPick, submitted)));

        MenuBuilder.Spacer(page);

        bool searching = !string.IsNullOrWhiteSpace(query);

        if (!searching && groups != null && groups.Count > 1)
        {
            AddCategories(title, page, groups, onPick);
        }
        else
        {
            AddEntries(page, searching ? CatalogSearch.Filter(all, query) : all, onPick);
        }

        return page;
    }

    private static void AddCategories<TPayload>(
        string title,
        REPOPopupPage page,
        IReadOnlyList<CatalogGroup<TPayload>> groups,
        Func<CatalogEntry<TPayload>, bool> onPick)
    {
        foreach (CatalogGroup<TPayload> group in groups)
        {
            CatalogGroup<TPayload> captured = group;
            MenuBuilder.Button(page, $"{captured.Category} ({captured.Count})", () =>
                MenuNavigator.Go(() => BuildCategory(title, captured, onPick)));
        }
    }

    private static REPOPopupPage BuildCategory<TPayload>(
        string title,
        CatalogGroup<TPayload> group,
        Func<CatalogEntry<TPayload>, bool> onPick)
    {
        REPOPopupPage page = MenuBuilder.CreatePage($"{title} - {group.Category}");
        AddEntries(page, group.Entries, onPick);
        return page;
    }

    private static void AddEntries<TPayload>(
        REPOPopupPage page,
        IReadOnlyList<CatalogEntry<TPayload>> entries,
        Func<CatalogEntry<TPayload>, bool> onPick)
    {
        foreach (CatalogEntry<TPayload> entry in entries)
        {
            CatalogEntry<TPayload> captured = entry;
            MenuBuilder.Button(page, Label(captured), () =>
            {
                if (onPick(captured) && AdminMenuSettings.CloseMenuOnSpawn.Value)
                {
                    AdminMenuController.Close();
                }
            });
        }
    }

    private static string Label<TPayload>(CatalogEntry<TPayload> entry)
    {
        return entry.IsModded
            ? $"({Strings.Get(StringKeys.SourceMod)}) {entry.DisplayName}"
            : entry.DisplayName;
    }
}
