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

    /// <summary>The code of the language the game is in, or English if it is not up yet.</summary>
    internal static string ActiveLocaleCode()
    {
        try
        {
            string code = LocalizationManager.instance?.ActiveLocale?.LocaleReference?.Identifier.Code;
            return string.IsNullOrEmpty(code) ? StringTables.FallbackLocaleCode : code;
        }
        catch (Exception)
        {
            return StringTables.FallbackLocaleCode;
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
