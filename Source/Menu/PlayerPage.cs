using AdminMenu.Actions;
using AdminMenu.Features;
using AdminMenu.Localization;
using AdminMenu.Targeting;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// One player: the switches first, then what opens another page, then the one-off actions.
///
/// The order is the point. Switches are a block of identical controls and read as a block;
/// mixing them in among buttons, which is what this page did at first, leaves a column where
/// nothing lines up with anything.
///
/// All three switches work for any player, but not all in the same way. Immortality the host
/// enforces itself for the damage it works out. Stamina and flight cannot be done to someone
/// from outside - they are worked out on that player's own machine - so what the host sends is
/// permission, and the player's own copy of the mod acts on it. A player without the mod never
/// hears it, and the switch here still reads as on, because what was given was permission.
/// </summary>
internal static class PlayerPage
{
    internal static REPOPopupPage Build(TargetPlayer player)
    {
        REPOPopupPage page = MenuBuilder.CreatePage(MenuTitle.For(player.Name));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerGodMode),
            AdminFlags.Has(AdminFlag.Immortal, player),
            state => AdminFlags.Grant(AdminFlag.Immortal, player, state));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerInvisible),
            InvisibilityActions.IsInvisible(player),
            state => InvisibilityActions.SetInvisible(player, state));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerInfiniteStamina),
            AdminFlags.Has(AdminFlag.InfiniteStamina, player),
            state => AdminFlags.Grant(AdminFlag.InfiniteStamina, player, state));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.PlayerFreeFlight),
            AdminFlags.Has(AdminFlag.FreeFlight, player),
            state => AdminFlags.Grant(AdminFlag.FreeFlight, player, state));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.PlayersUpgrades), () =>
            MenuNavigator.Go(() => UpgradesPage.Build(player)));
        MenuBuilder.Button(page, Strings.Get(StringKeys.PlayersTeleport), () =>
            MenuNavigator.Go(() => TeleportPage.Build(player)));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.PlayerHeal), () => PlayerActions.Heal(player));
        MenuBuilder.Button(page, Strings.Get(StringKeys.PlayerKill), () => PlayerActions.Kill(player));
        MenuBuilder.Button(page, Strings.Get(StringKeys.PlayerRevive), () => PlayerActions.Revive(player));

        return page;
    }
}
