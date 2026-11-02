using System;
using MenuLib;
using MenuLib.MonoBehaviors;
using UnityEngine;

namespace AdminMenu.Menu;

/// <summary>
/// Thin wrappers over MenuLib so a page reads as a list of its contents.
///
/// Controls go into the page's scroll view, which stacks them vertically by itself - no page
/// here works out positions, apart from <see cref="ButtonRow"/>, which is the one case where
/// two controls have to share a line.
///
/// Sliders are created without a description on purpose. A description turns a slider into a
/// two-line block with a caption under it, which for a list of twenty upgrades fills the page
/// with the word "level" repeated twenty times.
///
/// Toggles, sliders and text boxes are left at the size MenuLib gives them: they are clones of
/// the game's own settings rows and already look right. Only buttons and labels are resized,
/// because those two come from templates meant for something much larger - see
/// <see cref="MenuFont"/>.
/// </summary>
internal static class MenuBuilder
{
    /// <summary>
    /// Gap the scroll view leaves between every control. Zero, which is what the game's own
    /// pages use: anything above it is added to each of a dozen rows and the page stops
    /// fitting.
    /// </summary>
    private const float Spacing = 0f;

    /// <summary>Height of a deliberate gap between two groups of controls.</summary>
    private static readonly Vector2 SpacerSize = new Vector2(0f, 8f);

    /// <summary>Width and height of a button that shares a line with another one.</summary>
    private static readonly Vector2 RowButtonSize = new Vector2(110f, 26f);

    /// <summary>How far apart two buttons on one line sit.</summary>
    private const float RowStep = 125f;

    internal static REPOPopupPage CreatePage(string title)
    {
        REPOPopupPage page = MenuAPI.CreateREPOPopupPage(
            title,
            REPOPopupPage.PresetSide.Left,
            shouldCachePage: false,
            pageDimmerVisibility: true,
            spacing: Spacing);

        MenuFont.ApplyToHeader(page.headerTMP);
        return page;
    }

    internal static void Button(REPOPopupPage page, string text, Action onClick)
    {
        page.AddElementToScrollView(scrollView =>
        {
            REPOButton button = MenuAPI.CreateREPOButton(text, onClick, scrollView);
            MenuFont.Apply(button.labelTMP);
            return button.rectTransform;
        });
    }

    /// <summary>
    /// Several buttons on one line. MenuLib gives every control a line of its own, so the
    /// buttons go inside a container of a known size and the scroll view places that.
    /// </summary>
    internal static void ButtonRow(REPOPopupPage page, params (string Text, Action OnClick)[] buttons)
    {
        if (buttons == null || buttons.Length == 0)
        {
            return;
        }

        page.AddElementToScrollView(scrollView =>
        {
            GameObject container = new GameObject("Button Row", typeof(RectTransform));
            RectTransform rectTransform = (RectTransform)container.transform;
            rectTransform.SetParent(scrollView, worldPositionStays: false);
            rectTransform.sizeDelta = new Vector2(RowStep * buttons.Length, RowButtonSize.y);

            for (int i = 0; i < buttons.Length; i++)
            {
                REPOButton button = MenuAPI.CreateREPOButton(
                    buttons[i].Text,
                    buttons[i].OnClick,
                    rectTransform,
                    new Vector2(RowStep * i, 0f));

                MenuFont.Apply(button.labelTMP);

                // Without a fixed size each button is as wide as its own text, and the line
                // reads as a ragged row rather than a pair.
                button.overrideButtonSize = RowButtonSize;
            }

            return rectTransform;
        });
    }

    internal static void Toggle(REPOPopupPage page, string text, bool value, Action<bool> onToggle)
    {
        page.AddElementToScrollView(scrollView =>
        {
            REPOToggle toggle = MenuAPI.CreateREPOToggle(text, onToggle, scrollView, defaultValue: value);
            return toggle.rectTransform;
        });
    }

    internal static void Label(REPOPopupPage page, string text)
    {
        float width = page.maskRectTransform == null ? 0f : page.maskRectTransform.sizeDelta.x;

        page.AddElementToScrollView(scrollView =>
        {
            REPOLabel label = MenuAPI.CreateREPOLabel(text, scrollView);
            MenuFont.ApplyToLabel(label, width);
            return label.rectTransform;
        });
    }

    /// <summary>
    /// A whole-number field with arrows, which is what the game uses for every numeric
    /// setting. Used for upgrade levels, the level number and the money.
    /// </summary>
    internal static void IntSlider(
        REPOPopupPage page,
        string text,
        int min,
        int max,
        int value,
        Action<int> onChanged)
    {
        page.AddElementToScrollView(scrollView =>
        {
            REPOSlider slider = MenuAPI.CreateREPOSlider(
                text,
                string.Empty,
                onChanged,
                scrollView,
                min: min,
                max: max,
                defaultValue: Mathf.Clamp(value, min, max));

            return slider.rectTransform;
        });
    }

    /// <summary>
    /// A row of names with arrows, the same control the numbers use but stepping through a
    /// list instead of counting. The field itself cannot be typed into - the arrows are the
    /// only way to change it - which is what makes it right for picking one of a known few.
    ///
    /// The callback gets the position in the list rather than the name, so two players called
    /// the same thing are still two different answers.
    /// </summary>
    internal static void OptionSlider(
        REPOPopupPage page,
        string text,
        string[] options,
        int selected,
        Action<int> onChanged)
    {
        if (options == null || options.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(selected, 0, options.Length - 1);

        page.AddElementToScrollView(scrollView =>
            MenuAPI.CreateREPOSlider(
                text,
                string.Empty,
                onChanged,
                scrollView,
                options,
                options[index]).rectTransform);
    }

    /// <summary>
    /// A text box that reports only on Enter. Pages redraw themselves from the new value, so
    /// reporting every keystroke would redraw the page once per letter.
    /// </summary>
    internal static void InputField(REPOPopupPage page, string label, string value, Action<string> onSubmit)
    {
        page.AddElementToScrollView(scrollView =>
        {
            REPOInputField field = MenuAPI.CreateREPOInputField(
                label,
                onSubmit,
                scrollView,
                onlyNotifyOnSubmit: true,
                placeholder: label,
                defaultValue: value ?? string.Empty);

            return field.rectTransform;
        });
    }

    internal static void Spacer(REPOPopupPage page)
    {
        // A spacer has no size of its own, so without one it is a gap of nothing.
        page.AddElementToScrollView(scrollView =>
            MenuAPI.CreateREPOSpacer(scrollView, size: SpacerSize).rectTransform);
    }
}
