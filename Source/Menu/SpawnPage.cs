using AdminMenu.Actions;
using AdminMenu.Catalog;
using AdminMenu.Localization;
using AdminMenu.Targeting;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The four things that can be spawned. Each opens the same list page, which is why there is
/// one of those rather than a page per category and per valuable size.
///
/// Each catalog is built when its list is opened rather than when the menu is, because
/// reading enemy names costs a prefab load each and paying for all of it on every menu open
/// is what made the menu take seconds to appear.
///
/// Enemies have a page of their own: how many to spawn belongs with the enemy list and
/// nowhere else.
///
/// Who the spawn appears next to is chosen at the top of this page, by stepping through the
/// players with the arrows. It used to be set by opening a player on the Players tab, which is
/// a page about doing things to that player - nothing there says "and everything you spawn
/// from now on lands by them".
/// </summary>
internal static class SpawnPage
{
    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.ForKey(StringKeys.TabSpawn));

        PlayerOptions targets = PlayerOptions.Build(TargetPlayerService.Current);

        MenuBuilder.OptionSlider(
            page,
            Strings.Get(StringKeys.SpawnTarget),
            targets.Labels,
            targets.SelectedIndex,
            index => TargetPlayerService.Select(targets.At(index)));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.SpawnItems), () =>
        {
            ItemCatalog.EnsureBuilt();
            CatalogListPage.Open(
                MenuTitle.ForKey(StringKeys.TabSpawn, StringKeys.SpawnItems),
                ItemCatalog.Groups,
                ItemCatalog.All,
                SpawnActions.SpawnItem);
        });

        MenuBuilder.Button(page, Strings.Get(StringKeys.SpawnValuables), () =>
        {
            ValuableCatalog.EnsureBuilt();
            CatalogListPage.Open(
                MenuTitle.ForKey(StringKeys.TabSpawn, StringKeys.SpawnValuables),
                ValuableCatalog.Groups,
                ValuableCatalog.All,
                SpawnActions.SpawnValuable);
        });

        MenuBuilder.Button(page, Strings.Get(StringKeys.SpawnEnemies), EnemyListPage.Open);

        MenuBuilder.Button(page, Strings.Get(StringKeys.SpawnCosmeticBoxes), () =>
        {
            CosmeticBoxCatalog.EnsureBuilt();
            CatalogListPage.Open(
                MenuTitle.ForKey(StringKeys.TabSpawn, StringKeys.SpawnCosmeticBoxes),
                null,
                CosmeticBoxCatalog.All,
                SpawnActions.SpawnCosmeticBox);
        });

        return page;
    }
}
