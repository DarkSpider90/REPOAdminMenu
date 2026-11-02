using System;

namespace AdminMenu.Localization;

/// <summary>
/// The menu's text in the language the game is set to.
///
/// The locale code comes from the game itself, so the mod does not keep a list of languages:
/// whatever <c>LocalizationManager</c> has selected is looked up, then the base language of a
/// regional code, then English. A key with no text anywhere comes back as the key, which is
/// ugly on screen and therefore easy to spot and fix.
/// </summary>
internal static class Strings
{
    internal static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        string code = ActiveLocaleCode();

        if (StringTables.TryGet(code, key, out string text))
        {
            return text;
        }

        // "pt-BR" falls back to "pt" before it falls back to English.
        string baseCode = BaseLocaleCode(code);
        if (baseCode != null && StringTables.TryGet(baseCode, key, out text))
        {
            return text;
        }

        return StringTables.TryGet(StringTables.FallbackLocaleCode, key, out text) ? text : key;
    }

    /// <summary>
    /// Text in the game's language only - no English fallback, and nothing at all while the
    /// game is in English. For names the game ships in English: when there is no translation
    /// the caller keeps the game's own name rather than taking ours.
    /// </summary>
    internal static bool TryGetLocal(string key, out string text)
    {
        text = null;
        string code = ActiveLocaleCode();
        string baseCode = BaseLocaleCode(code);

        if (string.Equals(baseCode ?? code, StringTables.FallbackLocaleCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return StringTables.TryGet(code, key, out text) || StringTables.TryGet(baseCode, key, out text);
    }

    internal static string Format(string key, params object[] args)
    {
        string text = Get(key);

        try
        {
            return string.Format(text, args);
        }
        catch (FormatException)
        {
            // A translation with a broken placeholder should not take the menu down.
            Plugin.Log.LogWarning($"Text for \"{key}\" has a bad placeholder: {text}");
            return text;
        }
    }

    /// <summary>
    /// A finished line as it should be handed to TextMeshPro. Only right-to-left languages
    /// change anything; applied once, where text leaves the mod, because lines are put
    /// together from the menu's words and the game's names.
    /// </summary>
    internal static string ForDisplay(string text)
    {
        return ArabicText.IsActive ? ArabicText.ToVisual(text) : text;
    }

    /// <summary>The menu's own label for a cosmetic box rarity.</summary>
    internal static string ForRarity(SemiFunc.Rarity rarity)
    {
        switch (rarity)
        {
            case SemiFunc.Rarity.Common: return Get(StringKeys.RarityCommon);
            case SemiFunc.Rarity.Uncommon: return Get(StringKeys.RarityUncommon);
            case SemiFunc.Rarity.Rare: return Get(StringKeys.RarityRare);
            case SemiFunc.Rarity.UltraRare: return Get(StringKeys.RarityUltraRare);
            default: return GameText.Humanize(rarity.ToString());
        }
    }

    /// <summary>The menu's own label for a kind of shop item.</summary>
    internal static string ForItemType(SemiFunc.itemType type)
    {
        return ForEnum(StringKeys.ItemTypePrefix, type.ToString());
    }

    /// <summary>The menu's own label for a valuable's size.</summary>
    internal static string ForValuableSize(ValuableVolume.Type size)
    {
        return ForEnum(StringKeys.ValuableSizePrefix, size.ToString());
    }

    /// <summary>
    /// Text for one value of a game enum. A value the tables do not know - a game update
    /// added it - is spaced out from its name rather than shown as a raw key.
    /// </summary>
    private static string ForEnum(string prefix, string valueName)
    {
        string key = prefix + valueName;
        string text = Get(key);
        return text == key ? GameText.Humanize(valueName) : text;
    }

    /// <summary>
    /// The code of the language the game is in, or English if it is not up yet.
    ///
    /// Read the way the game reads it itself (<c>SemiFunc.LocaleGetCode</c>, which is the
    /// localization system's selected locale). <c>LocalizationManager.ActiveLocale</c> is not
    /// used: it keeps its English default until the language is switched in the settings, so
    /// a game that started in Russian - from the save or the system language - reads as English.
    /// </summary>
    internal static string ActiveLocaleCode()
    {
        try
        {
            string code = SemiFunc.LocaleGetCode();
            return string.IsNullOrEmpty(code) ? StringTables.FallbackLocaleCode : code;
        }
        catch (Exception)
        {
            return StringTables.FallbackLocaleCode;
        }
    }

    /// <summary>The game's settings for the language it is in, or null.</summary>
    internal static LocaleAsset ActiveLocaleAsset()
    {
        try
        {
            LocalizationManager manager = LocalizationManager.instance;
            return manager != null && manager.Locales.TryGetValue(ActiveLocaleCode(), out LocaleAsset locale)
                ? locale
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string BaseLocaleCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        int separator = code.IndexOf('-');
        return separator > 0 ? code.Substring(0, separator) : null;
    }
}
