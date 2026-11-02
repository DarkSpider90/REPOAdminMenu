using System;
using System.Reflection;
using AdminMenu.Configuration;
using HarmonyLib;
using MenuLib.MonoBehaviors;
using TMPro;
using UnityEngine;

namespace AdminMenu.Menu;

/// <summary>
/// Gives the whole menu the text size of a settings row.
///
/// MenuLib builds its controls from three different places in the game's own interface, and
/// they are not the same size. A toggle and a slider are cloned from the audio settings page,
/// which is where the upgrade rows get their compact look. A button is cloned from "Quit game"
/// on the main menu, which is a title-sized button meant to be read across the room. A label
/// is worse still: MenuLib sets it to font 30 in a box 200 wide, so any line longer than a word
/// or two is drawn oversized and then cut off - which is what happened to the target line.
///
/// Rather than pick a number, the size is read from MenuLib's own settings-row template, so
/// the buttons end up at exactly the size of the rows that already look right. If that cannot
/// be read, nothing is changed and the menu looks as it did. <c>MenuFontSize</c> in the config
/// overrides it either way.
/// </summary>
internal static class MenuFont
{
    private const float MinSize = 8f;
    private const float MaxSize = 32f;

    /// <summary>Headers shrink down to this before they are allowed to wrap.</summary>
    private const float MinHeaderSize = 11f;

    private static float? _rowFontSize;

    /// <summary>
    /// The size a settings row uses, or zero when it could not be read. Taken from the toggle
    /// template MenuLib keeps, which is the game's own "Push to Talk" row.
    /// </summary>
    private static float RowFontSize
    {
        get
        {
            _rowFontSize ??= ReadTemplateFontSize();
            return _rowFontSize.Value;
        }
    }

    /// <summary>The size every label in the menu should be, or zero to leave it alone.</summary>
    private static float Wanted
    {
        get
        {
            int configured = AdminMenuSettings.MenuFontSize == null ? 0 : AdminMenuSettings.MenuFontSize.Value;
            return configured > 0 ? Mathf.Clamp(configured, MinSize, MaxSize) : RowFontSize;
        }
    }

    internal static void Apply(TextMeshProUGUI label)
    {
        float wanted = Wanted;
        if (label == null || wanted <= 0f)
        {
            return;
        }

        label.enableAutoSizing = false;
        label.fontSize = wanted;
    }

    /// <summary>
    /// Labels need their box resized as well as their text. MenuLib gives every label the same
    /// 200-wide box, which a sentence does not fit in, so it is widened to the page.
    /// </summary>
    internal static void ApplyToLabel(REPOLabel label, float availableWidth)
    {
        if (label == null)
        {
            return;
        }

        Apply(label.labelTMP);

        float height = Mathf.Max(label.labelTMP.fontSize + 4f, 16f);
        Vector2 size = new Vector2(Mathf.Max(availableWidth, 200f), height);

        label.rectTransform.sizeDelta = size;
        label.labelTMP.rectTransform.sizeDelta = size;
    }

    /// <summary>
    /// Keeps a page header on one line. The trail a header carries - the mod, the player, the
    /// page - gets long, and wrapped onto a second line it is drawn straight over the first row
    /// of the page. Shrinking it to fit is the one thing that cannot collide with anything.
    /// </summary>
    internal static void ApplyToHeader(TextMeshProUGUI header)
    {
        if (header == null)
        {
            return;
        }

        float largest = header.enableAutoSizing ? header.fontSizeMax : header.fontSize;
        if (largest <= MinHeaderSize)
        {
            return;
        }

        header.textWrappingMode = TextWrappingModes.NoWrap;
        header.enableAutoSizing = true;
        header.fontSizeMin = MinHeaderSize;
        header.fontSizeMax = largest;
    }

    private static float ReadTemplateFontSize()
    {
        try
        {
            Type templates = AccessTools.TypeByName("MenuLib.REPOTemplates");
            FieldInfo field = templates == null ? null : AccessTools.Field(templates, "toggleTemplate");

            if (field?.GetValue(null) is not RectTransform template)
            {
                return 0f;
            }

            TextMeshProUGUI label = template.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                return 0f;
            }

            // Auto-sized labels report the size they happen to be drawn at; the floor is the
            // size at which the longest of them still fits.
            float size = label.enableAutoSizing && label.fontSizeMin > 1f ? label.fontSizeMin : label.fontSize;
            return size > MinSize ? Mathf.Clamp(size, MinSize, MaxSize) : 0f;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(
                $"Could not read the size of a settings row, so the menu keeps MenuLib's own sizes. {ex.Message}");
            return 0f;
        }
    }
}
