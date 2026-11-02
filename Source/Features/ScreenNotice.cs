using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdminMenu.Features;

/// <summary>
/// A line of text across the top of the screen, for things the player has to see while they
/// are playing rather than while they are in the menu.
///
/// It is its own canvas above everything else, and it counts in unscaled time. That matters:
/// the menu is open when a countdown starts, and anything timed in ordinary game time stops
/// moving while the game is paused - which is how a message ends up on screen for good.
///
/// The font is borrowed from whatever text the game already has on screen, because a
/// TextMeshPro label with no font asset draws nothing at all.
/// </summary>
internal sealed class ScreenNotice : MonoBehaviour
{
    private const float FadeTime = 0.25f;

    private static ScreenNotice _instance;

    private CanvasGroup _group;
    private TextMeshProUGUI _label;
    private TextMeshProUGUI _shadow;
    private Coroutine _running;

    private static readonly Color TextColor = new Color(1f, 0.6f, 0.1f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.75f);

    /// <summary>Shows a line for a while. A new line replaces whatever was showing.</summary>
    internal static void Show(string text, float seconds)
    {
        ScreenNotice notice = Ensure();
        if (notice == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        notice.Display(text, seconds);
    }

    /// <summary>Shows a line until it is replaced or explicitly cleared.</summary>
    internal static void Hold(string text)
    {
        Show(text, 0f);
    }

    internal static void Clear()
    {
        if (_instance != null)
        {
            _instance.Display(null, 0f);
        }
    }

    private static ScreenNotice Ensure()
    {
        if (_instance != null)
        {
            return _instance;
        }

        try
        {
            GameObject host = new GameObject("AdminMenu Screen Notice");
            DontDestroyOnLoad(host);

            Canvas canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;

            CanvasScaler scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _instance = host.AddComponent<ScreenNotice>();
            _instance.Build();
            return _instance;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not create the on-screen notice. {ex.Message}");
            return null;
        }
    }

    private void Build()
    {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;
        _group.alpha = 0f;

        _shadow = CreateText("Notice Shadow", ShadowColor, new Vector2(2f, -2f));
        _label = CreateText("Notice", TextColor, Vector2.zero);
    }

    private TextMeshProUGUI CreateText(string name, Color color, Vector2 offset)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(transform, worldPositionStays: false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = 44f;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_FontAsset font = FindFont();
        if (font != null)
        {
            text.font = font;
        }

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(1200f, 70f);
        rectTransform.anchoredPosition = new Vector2(0f, -70f) + offset;

        return text;
    }

    /// <summary>
    /// Any font the game is already drawing with. Built without one, the label is there but
    /// invisible, which is a hard thing to work out from a bug report.
    /// </summary>
    private static TMP_FontAsset FindFont()
    {
        TextMeshProUGUI[] existing = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        if (existing == null)
        {
            return null;
        }

        foreach (TextMeshProUGUI candidate in existing)
        {
            if (candidate != null && candidate.font != null)
            {
                return candidate.font;
            }
        }

        return null;
    }

    private void Display(string text, float seconds)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        if (string.IsNullOrEmpty(text))
        {
            _group.alpha = 0f;
            return;
        }

        _label.text = text;
        _shadow.text = text;
        _group.alpha = 1f;

        if (seconds > 0f)
        {
            _running = StartCoroutine(HideAfter(seconds));
        }
    }

    private IEnumerator HideAfter(float seconds)
    {
        // Unscaled, so a paused game does not leave the line on screen for ever.
        yield return new WaitForSecondsRealtime(seconds);

        float elapsed = 0f;
        while (elapsed < FadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01(1f - elapsed / FadeTime);
            yield return null;
        }

        _group.alpha = 0f;
        _running = null;
    }
}
