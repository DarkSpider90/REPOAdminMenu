using System;
using System.Collections.Generic;
using System.Reflection;
using AdminMenu.Access;
using HarmonyLib;
using UnityEngine;

namespace AdminMenu.Actions;

/// <summary>
/// Marks everything worth picking up on the map.
///
/// Valuables and cosmetic boxes each have their own reveal: a valuable has
/// <c>ValuableObject.Discover</c>, a box carries a <c>ValuableDiscoverCustom</c> with a
/// <c>Discover</c> of its own. Both keep a <c>discovered</c> flag the game sets, read here
/// through reflection only so that something already found is left alone and the count the
/// menu reports is honest.
/// </summary>
internal static class RevealActions
{
    private static readonly FieldInfo ValuableDiscoveredField =
        AccessTools.Field(typeof(ValuableObject), "discovered");
    private static readonly FieldInfo CustomDiscoveredField =
        AccessTools.Field(typeof(ValuableDiscoverCustom), "discovered");

    /// <summary>Reveals every valuable in the level that has not been found yet.</summary>
    internal static int RevealValuables()
    {
        if (!Allowed())
        {
            return 0;
        }

        int revealed = 0;

        foreach (ValuableObject valuable in FindActive<ValuableObject>())
        {
            if (IsDiscovered(ValuableDiscoveredField, valuable))
            {
                continue;
            }

            try
            {
                valuable.Discover(ValuableDiscoverGraphic.State.Discover);
                revealed++;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not reveal \"{valuable.name}\". {ex.Message}");
            }
        }

        Plugin.LogVerbose($"Revealed {revealed} valuables.");
        return revealed;
    }

    /// <summary>Reveals every cosmetic box in the level that has not been found yet.</summary>
    internal static int RevealCosmeticBoxes()
    {
        if (!Allowed())
        {
            return 0;
        }

        int revealed = 0;

        foreach (CosmeticWorldObject box in CosmeticBoxes())
        {
            ValuableDiscoverCustom discover = box.GetComponent<ValuableDiscoverCustom>();
            if (discover == null || IsDiscovered(CustomDiscoveredField, discover))
            {
                continue;
            }

            try
            {
                if (discover.Discover())
                {
                    revealed++;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not reveal the cosmetic box \"{box.name}\". {ex.Message}");
            }
        }

        Plugin.LogVerbose($"Revealed {revealed} cosmetic boxes.");
        return revealed;
    }

    /// <summary>
    /// The boxes in the level. The round director keeps a list of them, and a plain search is
    /// the fallback for the moment before the list is filled.
    /// </summary>
    private static IEnumerable<CosmeticWorldObject> CosmeticBoxes()
    {
        List<CosmeticWorldObject> boxes = new List<CosmeticWorldObject>();

        List<CosmeticWorldObject> fromDirector = RoundDirector.instance?.cosmeticWorldObjects;
        if (fromDirector != null && fromDirector.Count > 0)
        {
            foreach (CosmeticWorldObject box in fromDirector)
            {
                if (IsLive(box))
                {
                    boxes.Add(box);
                }
            }

            return boxes;
        }

        return FindActive<CosmeticWorldObject>();
    }

    private static List<T> FindActive<T>() where T : Component
    {
        List<T> found = new List<T>();

        // Unsorted: the game would otherwise order these by instance id, which costs time
        // nothing here needs.
        T[] all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (all == null)
        {
            return found;
        }

        foreach (T candidate in all)
        {
            if (IsLive(candidate))
            {
                found.Add(candidate);
            }
        }

        return found;
    }

    private static bool IsLive(Component component)
    {
        return component != null &&
               component.gameObject.activeInHierarchy &&
               (component is not Behaviour behaviour || behaviour.isActiveAndEnabled);
    }

    private static bool IsDiscovered(FieldInfo field, object instance)
    {
        if (field == null)
        {
            return false;
        }

        try
        {
            return field.GetValue(instance) is bool value && value;
        }
        catch (Exception)
        {
            // Worst case something already found is revealed again, which is harmless.
            return false;
        }
    }

    private static bool Allowed()
    {
        if (AdminAccess.CanAct)
        {
            return true;
        }

        Plugin.Log.LogWarning("Only the host can reveal the map.");
        return false;
    }
}
