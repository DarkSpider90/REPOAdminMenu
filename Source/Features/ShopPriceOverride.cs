using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace AdminMenu.Features;

/// <summary>
/// Makes what is already on the shelves free, and puts the prices back afterwards.
///
/// The patch on <c>ItemAttributes.GetValue</c> only catches items as the shop is built, so on
/// its own the switch would do nothing until the next shop. This does the other half: it
/// walks the items that are already out, remembers what each one cost and zeroes it, then
/// tells the other clients and asks the game to add the totals up again.
///
/// The original prices are kept so the switch works both ways. Without that, turning it off
/// would leave a shop full of free items until the next one was generated.
/// </summary>
internal static class ShopPriceOverride
{
    private static readonly FieldInfo ValueField = AccessTools.Field(typeof(ItemAttributes), "value");

    /// <summary>Price each item had before it was made free, by object id.</summary>
    private static readonly Dictionary<int, int> OriginalPrices = new Dictionary<int, int>();

    private static bool _reportedMissingField;

    /// <summary>Zeroes or restores every price in the level. Returns how many items changed.</summary>
    internal static int Apply(bool free)
    {
        if (ValueField == null)
        {
            Report();
            return 0;
        }

        int changed = 0;

        foreach (ItemAttributes item in ShopItems())
        {
            int id = item.GetInstanceID();

            if (free)
            {
                int current = ReadValue(item);
                if (current == 0)
                {
                    continue;
                }

                OriginalPrices[id] = current;
                if (WriteValue(item, 0))
                {
                    changed++;
                }
            }
            else
            {
                if (!OriginalPrices.TryGetValue(id, out int original))
                {
                    continue;
                }

                OriginalPrices.Remove(id);
                if (WriteValue(item, original))
                {
                    changed++;
                }
            }
        }

        if (!free)
        {
            // Anything that has since been destroyed will never be restored, so the rest is
            // dead weight.
            OriginalPrices.Clear();
        }

        UpdateShopTotals();
        return changed;
    }

    /// <summary>Drops the remembered prices. Called when a run ends.</summary>
    internal static void Forget()
    {
        OriginalPrices.Clear();
    }

    private static List<ItemAttributes> ShopItems()
    {
        List<ItemAttributes> items = new List<ItemAttributes>();

        // Unsorted: the game would otherwise order these by instance id for nothing.
        ItemAttributes[] all = UnityEngine.Object.FindObjectsByType<ItemAttributes>(FindObjectsSortMode.None);
        if (all == null)
        {
            return items;
        }

        foreach (ItemAttributes item in all)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static int ReadValue(ItemAttributes item)
    {
        try
        {
            return ValueField.GetValue(item) is int value ? value : 0;
        }
        catch (Exception ex)
        {
            Report(ex);
            return 0;
        }
    }

    /// <summary>
    /// Writes the price and sends it on, the same way the game does when it works a price out.
    /// </summary>
    private static bool WriteValue(ItemAttributes item, int value)
    {
        try
        {
            ValueField.SetValue(item, value);
        }
        catch (Exception ex)
        {
            Report(ex);
            return false;
        }

        if (!SemiFunc.IsMultiplayer())
        {
            return true;
        }

        PhotonView photonView = item.GetComponent<PhotonView>();
        if (photonView != null && photonView.ViewID != 0)
        {
            photonView.RPC("GetValueRPC", RpcTarget.Others, value);
        }

        return true;
    }

    /// <summary>Asks the game to add the shopping list up again with the new prices.</summary>
    private static void UpdateShopTotals()
    {
        try
        {
            SemiFunc.ShopUpdateCost();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not refresh the shop totals. {ex.Message}");
        }
    }

    private static void Report(Exception ex = null)
    {
        if (_reportedMissingField)
        {
            return;
        }

        _reportedMissingField = true;
        Plugin.Log.LogWarning(
            "ItemAttributes.value could not be reached, so prices can only be changed as a shop " +
            "is built. " + (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
