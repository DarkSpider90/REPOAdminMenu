using AdminMenu.Access;
using AdminMenu.Actions;
using AdminMenu.Catalog;
using AdminMenu.Targeting;
using AdminMenu.Upgrades;

namespace AdminMenu.Features;

/// <summary>
/// Clears everything the menu is holding when a run ends.
///
/// All of this state is about one run and one group of players: who the target was, who was
/// made immortal, which level was queued up, who had been granted access. Carrying any of it
/// into the next lobby would mean a player inheriting a switch somebody else set for someone
/// with the same slot, so it all goes at once and in one place.
/// </summary>
internal static class AdminSession
{
    internal static void Reset()
    {
        AdminFlags.Reset();
        ShopPriceOverride.Forget();
        SpawnedEnemyGuard.Clear();
        Menu.EnemyListPage.Reset();
        TargetPlayerService.Reset();
        AdminAccess.Reset();
        UpgradeDictionaryLink.Reset();
        LevelActions.ClearPendingLevel();
        ContentCatalog.Invalidate();

        Plugin.LogVerbose("Admin menu state cleared for a new run.");
    }
}
