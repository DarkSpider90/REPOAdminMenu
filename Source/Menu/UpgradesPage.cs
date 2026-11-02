using AdminMenu.Catalog;
using AdminMenu.Configuration;
using AdminMenu.Localization;
using AdminMenu.Targeting;
using AdminMenu.Upgrades;
using MenuLib.MonoBehaviors;

namespace AdminMenu.Menu;

/// <summary>
/// Every upgrade the game or another mod has, as one row each.
///
/// The row carries an absolute level and <see cref="UpgradeApplier"/> works out the rest, so
/// winding a row down removes levels through the same code that granted them. The top of the
/// range is the mod's own setting: the game has no cap, and the arrows have to stop somewhere.
/// </summary>
internal static class UpgradesPage
{
    internal static REPOPopupPage Build(TargetPlayer player)
    {
        UpgradeCatalog.EnsureBuilt();

        REPOPopupPage page = MenuBuilder.CreatePage(
            MenuTitle.For(player.Name, Strings.Get(StringKeys.PlayersUpgrades)));

        int max = AdminMenuSettings.MaxUpgradeLevel.Value;

        foreach (CatalogEntry<string> upgrade in UpgradeCatalog.All)
        {
            CatalogEntry<string> captured = upgrade;
            int level = UpgradeLevelReader.GetLevel(captured.Id, player);

            MenuBuilder.IntSlider(
                page,
                Label(captured),
                min: 0,
                max: max,
                value: level,
                onChanged: value => UpgradeApplier.SetLevel(captured.Id, player, value));
        }

        MenuBuilder.Spacer(page);
        MenuBuilder.Button(page, Strings.Get(StringKeys.UpgradesClearAll), () =>
        {
            UpgradeApplier.ClearAll(player);
            MenuNavigator.Refresh();
        });

        return page;
    }

    private static string Label(CatalogEntry<string> upgrade)
    {
        return upgrade.IsModded
            ? $"({Strings.Get(StringKeys.SourceMod)}) {upgrade.DisplayName}"
            : upgrade.DisplayName;
    }
}
