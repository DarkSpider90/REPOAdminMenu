using System;
using System.Text;

namespace AdminMenu.Localization;

/// <summary>
/// Turns the game's own names into something the menu can print.
///
/// Items, enemies and the vanilla upgrades all carry a <see cref="LocalizedAsset"/>, so the
/// menu shows them in whatever language the player picked rather than in a list of English
/// strings baked into this mod. Everything falls back a step at a time, because a modded
/// item may have no localized asset and a valuable has no name field at all.
/// </summary>
internal static class GameText
{
    /// <summary>
    /// The localized string, or <paramref name="fallback"/> when there is no asset, no locale
    /// selected yet, or no entry in the table.
    /// </summary>
    internal static string Resolve(LocalizedAsset localized, string fallback)
    {
        if (localized == null)
        {
            return fallback ?? string.Empty;
        }

        try
        {
            string text = localized.GetLocalizedString();
            return string.IsNullOrWhiteSpace(text) ? fallback ?? string.Empty : text;
        }
        catch (Exception)
        {
            // A table that is still loading throws rather than returning null.
            return fallback ?? string.Empty;
        }
    }

    /// <summary>
    /// Readable name for a prefab. Prefabs are named after their kind first
    /// ("Valuable Toy Train", "Enemy - Runner"), which reads badly in a list, so the kind
    /// prefix goes and the rest is spaced out.
    /// </summary>
    internal static string FromPrefabName(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return string.Empty;
        }

        string name = prefabName.Trim();
        foreach (string prefix in PrefabPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length).Trim();
                break;
            }
        }

        return Humanize(name);
    }

    /// <summary>
    /// Spaces out an identifier: "TumbleWings" becomes "Tumble Wings". Used for modded
    /// upgrades, whose id is all the mod gave us when it wrote no display name.
    /// </summary>
    internal static string Humanize(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return string.Empty;
        }

        string text = identifier.Replace('_', ' ').Replace('-', ' ').Trim();
        StringBuilder builder = new StringBuilder(text.Length + 8);

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            // A capital that follows a lower-case letter or a digit starts a new word, and so
            // does the last capital of a run before a lower-case letter ("HTTPServer").
            if (i > 0 && char.IsUpper(current) && builder.Length > 0 && builder[builder.Length - 1] != ' ')
            {
                char previous = text[i - 1];
                bool afterLower = char.IsLower(previous) || char.IsDigit(previous);
                bool endOfRun = char.IsUpper(previous) && i + 1 < text.Length && char.IsLower(text[i + 1]);

                if (afterLower || endOfRun)
                {
                    builder.Append(' ');
                }
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static readonly string[] PrefabPrefixes =
    {
        "Valuable ",
        "Level - ",
        "Enemy - ",
        "Enemy ",
        "Item ",
        "Cosmetic Pouch ",
        "Cosmetic ",
    };
}
