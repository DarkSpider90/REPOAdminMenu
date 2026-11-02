using AdminMenu.Actions;
using AdminMenu.Catalog;
using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The run itself: which level, how far along, the extraction points, and what is marked on
/// the map.
///
/// The level number is an ordinary numbered row, the same control the upgrades use, rather
/// than a label with a pair of arrow buttons beside it. One kind of numeric control for the
/// whole menu.
/// </summary>
internal static class LevelPage
{
    /// <summary>
    /// As high as the row goes. The game puts no ceiling on the level count, but the layout
    /// stops growing long before anyone reaches this.
    /// </summary>
    private const int MaxLevelsCompleted = 500;

    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.ForKey(StringKeys.TabLevel));

        MenuBuilder.IntSlider(
            page,
            Strings.Get(StringKeys.LevelCompleted),
            min: 0,
            max: MaxLevelsCompleted,
            value: LevelActions.LevelsCompleted,
            onChanged: completed => LevelActions.SetLevelsCompleted(completed));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelSelect), () =>
        {
            LevelCatalog.EnsureBuilt();
            CatalogListPage.Open(
                MenuTitle.ForKey(StringKeys.TabLevel, StringKeys.LevelSelect),
                LevelCatalog.Groups,
                LevelCatalog.All,
                LevelActions.ChangeTo);
        });

        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelShop), () => MenuNavigator.Go(ShopPage.Build));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelExtractionOpen), () => ExtractionActions.OpenNext());
        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelExtractionComplete), () => ExtractionActions.CompleteCurrent());

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelRevealValuables), () => RevealActions.RevealValuables());
        MenuBuilder.Button(page, Strings.Get(StringKeys.LevelRevealBoxes), () => RevealActions.RevealCosmeticBoxes());

        return page;
    }
}
