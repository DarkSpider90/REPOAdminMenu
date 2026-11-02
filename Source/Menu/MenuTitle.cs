using System.Text;
using AdminMenu.Localization;

namespace AdminMenu.Menu;

/// <summary>
/// Builds the trail that every page prints as its header: "Admin - Brent - Upgrades".
///
/// With one page visible at a time the header is the only thing telling the player where
/// they are and how they got there, so it carries the whole path rather than just the name
/// of the page.
/// </summary>
internal static class MenuTitle
{
    private const string Separator = " - ";

    /// <summary>The trail, starting from the mod's own short name.</summary>
    internal static string For(params string[] parts)
    {
        StringBuilder builder = new StringBuilder(Strings.Get(StringKeys.MenuShort));

        if (parts != null)
        {
            foreach (string part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    builder.Append(Separator).Append(part);
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>The trail for a page reached by key, which is most of them.</summary>
    internal static string ForKey(params string[] keys)
    {
        string[] parts = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            parts[i] = Strings.Get(keys[i]);
        }

        return For(parts);
    }
}
