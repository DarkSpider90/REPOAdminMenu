using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The page the menu opens on: the four places to go, and nothing else.
///
/// Which player the actions hit is shown where it is chosen - on the player list, where the
/// current one is marked - rather than repeated here. A line of it at the top of this page sat
/// under the header and was read as part of it.
/// </summary>
internal static class RootPage
{
    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.For());

        MenuBuilder.Button(page, Strings.Get(StringKeys.TabPlayers), () => MenuNavigator.Go(PlayersPage.Build));
        MenuBuilder.Button(page, Strings.Get(StringKeys.TabSpawn), () => MenuNavigator.Go(SpawnPage.Build));
        MenuBuilder.Button(page, Strings.Get(StringKeys.TabLevel), () => MenuNavigator.Go(LevelPage.Build));
        MenuBuilder.Button(page, Strings.Get(StringKeys.TabOptions), () => MenuNavigator.Go(OptionsPage.Build));

        return page;
    }
}
