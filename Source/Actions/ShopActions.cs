using AdminMenu.Access;
using AdminMenu.Features;
using UnityEngine;

namespace AdminMenu.Actions;

/// <summary>
/// The run's money, and the shop toggles.
///
/// Money goes through <c>SemiFunc.StatSetRunCurrency</c>, the same call the game's own
/// <c>/money</c> command makes: it is master-gated and sends the new amount to every client,
/// so there is nothing to patch and nothing to keep in step by hand. The game counts money in
/// thousands internally - a stat of 1 is $1000 on screen - so the menu works in the number
/// the player sees and converts here.
/// </summary>
internal static class ShopActions
{
    /// <summary>What one unit of the run's currency stat is worth on screen.</summary>
    private const int CurrencyUnit = 1000;

    /// <summary>Money as the player sees it.</summary>
    internal static int GetMoney()
    {
        return SemiFunc.StatGetRunCurrency() * CurrencyUnit;
    }

    /// <summary>Money as the game stores it, which is what a numbered row should show.</summary>
    internal static int GetMoneyInThousands()
    {
        return SemiFunc.StatGetRunCurrency();
    }

    internal static bool SetMoneyInThousands(int thousands)
    {
        return SetMoney(Mathf.Max(thousands, 0) * CurrencyUnit);
    }

    /// <summary>Sets money to this many dollars, rounded down to what the game can hold.</summary>
    internal static bool SetMoney(int dollars)
    {
        if (!Allowed())
        {
            return false;
        }

        int units = Mathf.Max(dollars, 0) / CurrencyUnit;
        SemiFunc.StatSetRunCurrency(units);

        Plugin.LogVerbose($"Money set to ${units * CurrencyUnit}.");
        return true;
    }

    internal static bool AddMoney(int dollars)
    {
        return SetMoney(GetMoney() + dollars);
    }

    /// <summary>True while the run is in the shop, which is when these controls matter.</summary>
    internal static bool InShop()
    {
        return SemiFunc.RunIsShop();
    }

    internal static bool SetFreeItems(bool state)
    {
        if (!Allowed())
        {
            return false;
        }

        AdminFlags.SetFreeShopItems(state);

        // The patch only catches items as a shop is built, so what is already on the shelves
        // is changed here - otherwise the switch would appear to do nothing until the next shop.
        int changed = ShopPriceOverride.Apply(state);
        Plugin.LogVerbose($"Free shop items {(state ? "on" : "off")}; {changed} prices changed.");
        return true;
    }

    private static bool Allowed()
    {
        if (AdminAccess.CanAct)
        {
            return true;
        }

        Plugin.Log.LogWarning("Only the host can change the run's money.");
        return false;
    }
}
