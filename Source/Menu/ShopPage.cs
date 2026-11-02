using AdminMenu.Actions;
using AdminMenu.Features;
using AdminMenu.Localization;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// The run's money and the free-items switch.
///
/// The money row works in thousands, which is how the game stores it - a stat of 1 is $1000
/// on screen - so the row reads as the number of thousands and the buttons underneath move it
/// in the steps a player actually wants.
///
/// Immortality is not repeated here: the switch on a player page already covers the shop, and
/// the game makes no distinction between being crushed in a level and being crushed between
/// the shelves.
/// </summary>
internal static class ShopPage
{
    /// <summary>What one press of the money buttons is worth on screen.</summary>
    private const int Step = 10000;

    /// <summary>As high as the money row goes; the stat itself is held in thousands.</summary>
    private const int MaxThousands = 9999;

    internal static REPOPopupPage Build()
    {
        REPOPopupPage page = MenuBuilder.CreatePage(
            MenuTitle.ForKey(StringKeys.TabLevel, StringKeys.LevelShop));

        MenuBuilder.Toggle(
            page,
            Strings.Get(StringKeys.LevelFreeItems),
            AdminFlags.FreeShopItems,
            state => ShopActions.SetFreeItems(state));

        MenuBuilder.IntSlider(
            page,
            Strings.Get(StringKeys.LevelMoney),
            min: 0,
            max: MaxThousands,
            value: ShopActions.GetMoneyInThousands(),
            onChanged: thousands => ShopActions.SetMoneyInThousands(thousands));

        MenuBuilder.ButtonRow(
            page,
            ($"+${Step}", () => Adjust(Step)),
            ($"-${Step}", () => Adjust(-Step)));

        return page;
    }

    private static void Adjust(int dollars)
    {
        if (ShopActions.AddMoney(dollars))
        {
            // The money row prints the amount, and a row does not update itself.
            MenuNavigator.Refresh();
        }
    }
}
