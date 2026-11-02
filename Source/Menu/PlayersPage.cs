using System.Collections.Generic;
using AdminMenu.Localization;
using AdminMenu.Targeting;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// Everyone in the lobby. Picking one opens that player's page, and does nothing else.
///
/// It used to quietly make them the target for spawning as well, which is not something a
/// list of players says it does; where a spawn lands is now asked on the Spawn tab.
/// </summary>
internal static class PlayersPage
{
    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.ForKey(StringKeys.TabPlayers));

        List<TargetPlayer> players = TargetPlayerService.ListPlayers();

        foreach (TargetPlayer player in players)
        {
            TargetPlayer captured = player;
            MenuBuilder.Button(page, PlayerLabel.For(captured), () => MenuNavigator.Go(() => PlayerPage.Build(captured)));
        }

        return page;
    }
}
