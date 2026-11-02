using AdminMenu.Actions;
using AdminMenu.Localization;
using AdminMenu.Targeting;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// Where this player can be sent.
///
/// "Me" is the host, always - not whoever the host is watching. Once the host is dead the
/// camera follows someone else, and an action aimed at the host meaning "that other person"
/// is a surprise nobody wants while sorting out a round. On the host's own page those two
/// read as "me to me", so they are not offered there at all.
///
/// Sending someone to a third player is picked with arrows rather than on a page of its own:
/// one row that cannot be typed into, and the button under it does the move.
///
/// Dead works everywhere here, on both sides. A dead player is where their head is, so they
/// can be sent somewhere and someone can be sent to them; moving one moves the head and the
/// ragdoll together, which is what the game's own revive follows.
/// </summary>
internal static class TeleportPage
{
    /// <summary>Where the arrows are pointing, kept while the menu is open.</summary>
    private static TargetPlayer _destination = TargetPlayer.None;

    internal static REPOPopupPage Build(TargetPlayer player)
    {
        REPOPopupPage page = MenuBuilder.CreatePage(
            MenuTitle.For(player.Name, Strings.Get(StringKeys.PlayersTeleport)));

        if (!TargetPlayerResolver.IsLocal(player))
        {
            MenuBuilder.Button(page, Strings.Get(StringKeys.TeleportPlayerToMe), () => PlayerActions.TeleportToMe(player));
            MenuBuilder.Button(page, Strings.Get(StringKeys.TeleportMeToPlayer), () => PlayerActions.TeleportMeTo(player));

            MenuBuilder.Spacer(page);
        }

        PlayerOptions destinations = PlayerOptions.Build(Destination());

        MenuBuilder.OptionSlider(
            page,
            Strings.Get(StringKeys.TeleportPlayerTo),
            destinations.Labels,
            destinations.SelectedIndex,
            index => _destination = destinations.At(index));

        MenuBuilder.Button(page, Strings.Get(StringKeys.TeleportGo), () =>
            PlayerActions.TeleportTo(player, Destination()));

        MenuBuilder.Spacer(page);

        MenuBuilder.Button(page, Strings.Get(StringKeys.TeleportToTruck), () => PlayerActions.TeleportToTruck(player));
        MenuBuilder.Button(page, Strings.Get(StringKeys.TeleportToExtraction), () => PlayerActions.TeleportToExtraction(player));

        return page;
    }

    /// <summary>Falls back to the host, so the row always points at somebody real.</summary>
    private static TargetPlayer Destination()
    {
        return _destination.IsValid && _destination.TryResolve(out _)
            ? _destination
            : TargetPlayerResolver.Local();
    }
}
