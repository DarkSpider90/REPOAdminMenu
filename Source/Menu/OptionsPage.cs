using AdminMenu.Features;
using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The switches that are not about one player.
///
/// Both of these work by the host changing what it simulates, so they hold for the whole
/// lobby: enemies belong to the host, and so does the charge in every battery it is keeping
/// track of. There is no per-player version of either - the game decides enemy damage and
/// battery drain in one place, not once per player - so they live here rather than on a
/// player page where they would read as a promise the mod cannot keep.
/// </summary>
internal static class OptionsPage
{
    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.ForKey(StringKeys.TabOptions));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerOneShotEnemies),
            AdminFlags.OneShotEnemies,
            AdminFlags.SetOneShotEnemies);

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerInfiniteAmmo),
            AdminFlags.InfiniteAmmo,
            AdminFlags.SetInfiniteAmmo);

        return page;
    }
}
