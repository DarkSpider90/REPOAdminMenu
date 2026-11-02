using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AdminMenu.Configuration;

namespace AdminMenu.Localization;

/// <summary>
/// Loads menu text for a language from a <c>.lang</c> file.
///
/// A language is a file rather than a code change: one <c>key=value</c> per line, <c>#</c>
/// for a comment, UTF-8, named after the locale code the game uses - "ru", "pt-BR". Every
/// language the game ships is built into the DLL this way (<c>Source/Localization/Languages</c>).
///
/// The player's own files live in <c>BepInEx/config/AdminMenu/Languages</c> and are read
/// afterwards, so a line there replaces the built-in one. They are kept under config rather
/// than beside the DLL because a mod manager replaces the mod's folder on every update, and
/// would take the player's corrections with it.
/// </summary>
internal static class LanguageFiles
{
    private const string FolderName = "Languages";
    private const string FileExtension = ".lang";
    private const string TemplateSuffix = ".template";

    /// <summary>Name prefix the project gives the built-in files; see AdminMenu.csproj.</summary>
    private const string ResourcePrefix = "AdminMenu.Languages.";

    /// <summary>How many unknown keys a warning names before it just counts the rest.</summary>
    private const int UnknownKeysShown = 5;

    private static readonly Regex Placeholder = new Regex(@"\{\d+\}", RegexOptions.Compiled);

    /// <summary>Loads the translations built into the mod.</summary>
    internal static void LoadEmbedded()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (string resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(FileExtension, StringComparison.Ordinal))
            {
                continue;
            }

            string localeCode = resource.Substring(
                ResourcePrefix.Length,
                resource.Length - ResourcePrefix.Length - FileExtension.Length);

            try
            {
                using Stream stream = assembly.GetManifestResourceStream(resource);
                if (stream == null)
                {
                    continue;
                }

                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StringTables.Merge(localeCode, Parse(reader));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not read the built-in language \"{localeCode}\". {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Loads the player's language files. The old folder beside the DLL is still read, so
    /// files from an earlier version keep working until the next update wipes them; the
    /// config folder is read last and wins.
    /// </summary>
    internal static void LoadAll()
    {
        string legacy = LegacyFolderPath();
        if (LoadFolder(legacy) > 0)
        {
            Plugin.Log.LogWarning(
                $"Language files beside the mod will be lost on the next update. Move them to \"{UserFolderPath()}\".");
        }

        LoadFolder(UserFolderPath());
    }

    /// <summary>
    /// Writes the menu as it reads in the game's current language, so a player who wants to
    /// change one line starts from the translation they see rather than from English. Called
    /// once the game has picked its language; switches the setting back off afterwards, so the
    /// file is written once rather than on every start.
    /// </summary>
    internal static void WriteTemplateIfRequested()
    {
        if (AdminMenuSettings.WriteLanguageTemplate == null || !AdminMenuSettings.WriteLanguageTemplate.Value)
        {
            return;
        }

        if (WriteTemplate(Strings.ActiveLocaleCode()))
        {
            AdminMenuSettings.WriteLanguageTemplate.Value = false;
        }
    }

    private static bool WriteTemplate(string localeCode)
    {
        string folder = UserFolderPath();
        if (folder == null)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, localeCode + TemplateSuffix + FileExtension);
            File.WriteAllText(path, TemplateText(localeCode), new UTF8Encoding(false));
            Plugin.Log.LogInfo($"Wrote the language template to \"{path}\".");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not write the language template. {ex.Message}");
            return false;
        }
    }

    private static string TemplateText(string localeCode)
    {
        LocaleAsset locale = Strings.ActiveLocaleAsset();
        string languageName = locale == null || string.IsNullOrEmpty(locale.NativeName) ? localeCode : locale.NativeName;
        bool english = Strings.IsEnglish(localeCode);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"# Admin Menu - {languageName} ({localeCode})");
        builder.AppendLine("# Every line of the menu as it reads right now.");
        builder.AppendLine("#");
        builder.AppendLine("# To change some of them:");
        builder.AppendLine($"#   1. Copy this file to \"{localeCode}{FileExtension}\" in this same folder.");
        builder.AppendLine("#   2. Delete the lines you do not want to change. Whatever you leave out keeps the mod's text.");
        builder.AppendLine("#   3. Edit the text after \"=\". Leave the part before it exactly as it is.");
        builder.AppendLine("#   4. Restart the game.");
        builder.AppendLine("#");
        builder.AppendLine("# {0} and {1} are filled in with names and numbers - keep them in the text.");
        builder.AppendLine("# This file itself is never read, and turning the setting on again rewrites it.");
        builder.AppendLine();

        foreach (KeyValuePair<string, string> pair in SortedEnglish())
        {
            // In English the menu shows the game's own names for these, so a line here
            // would change nothing.
            if (english && IsGameNameKey(pair.Key))
            {
                continue;
            }

            if (Strings.TryGetIn(localeCode, pair.Key, out string text))
            {
                builder.Append(pair.Key).Append('=').AppendLine(text);
            }
            else
            {
                builder.AppendLine("# Not translated yet, shown in English:");
                builder.Append(pair.Key).Append('=').AppendLine(pair.Value);
            }
        }

        return builder.ToString();
    }

    /// <summary>Loads every language file in one folder. Returns how many it loaded.</summary>
    private static int LoadFolder(string folder)
    {
        if (folder == null || !Directory.Exists(folder))
        {
            return 0;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(folder, "*" + FileExtension);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not list the language folder \"{folder}\". {ex.Message}");
            return 0;
        }

        int loaded = 0;
        foreach (string file in files)
        {
            string localeCode = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(localeCode) ||
                localeCode.EndsWith(TemplateSuffix, StringComparison.OrdinalIgnoreCase) ||
                localeCode.Equals("template", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Dictionary<string, string> entries = Validate(Path.GetFileName(file), Read(file));
            if (entries.Count > 0)
            {
                StringTables.Merge(localeCode, entries);
                Plugin.Log.LogInfo($"Loaded {entries.Count} menu strings for \"{localeCode}\" from \"{file}\".");
                loaded++;
            }
        }

        return loaded;
    }

    /// <summary>
    /// Drops what would go wrong on screen and says why. A key the mod does not know is most
    /// likely a typo, which would otherwise change nothing without a word. A line whose
    /// <c>{0}</c> markers differ from the English one would show a raw marker or lose a name,
    /// so the built-in text is kept instead.
    /// </summary>
    private static Dictionary<string, string> Validate(string fileName, Dictionary<string, string> entries)
    {
        Dictionary<string, string> valid = new Dictionary<string, string>(StringComparer.Ordinal);
        List<string> unknown = new List<string>();

        foreach (KeyValuePair<string, string> pair in entries)
        {
            if (!StringTables.Fallback.TryGetValue(pair.Key, out string english))
            {
                unknown.Add(pair.Key);
                continue;
            }

            if (PlaceholderSignature(pair.Value) != PlaceholderSignature(english))
            {
                Plugin.Log.LogWarning(
                    $"{fileName}: \"{pair.Key}\" must contain the same markers as the English text " +
                    $"(\"{english}\"), so the line is ignored.");
                continue;
            }

            valid[pair.Key] = pair.Value;
        }

        if (unknown.Count > 0)
        {
            string shown = string.Join(", ", unknown.GetRange(0, Math.Min(unknown.Count, UnknownKeysShown)));
            string more = unknown.Count > UnknownKeysShown ? $" and {unknown.Count - UnknownKeysShown} more" : string.Empty;
            Plugin.Log.LogWarning(
                $"{fileName}: {unknown.Count} line(s) have a key the mod does not know, probably a typo: {shown}{more}.");
        }

        return valid;
    }

    private static string PlaceholderSignature(string text)
    {
        List<string> markers = new List<string>();
        foreach (Match match in Placeholder.Matches(text))
        {
            markers.Add(match.Value);
        }

        markers.Sort(StringComparer.Ordinal);
        return string.Join(",", markers);
    }

    private static bool IsGameNameKey(string key)
    {
        return key.StartsWith(StringKeys.ValuableNamePrefix, StringComparison.Ordinal) ||
               key.StartsWith(StringKeys.ValuableThemePrefix, StringComparison.Ordinal) ||
               key.StartsWith(StringKeys.LevelNamePrefix, StringComparison.Ordinal) ||
               key == StringKeys.ValuableNameFormat;
    }

    private static Dictionary<string, string> Read(string path)
    {
        try
        {
            using StreamReader reader = new StreamReader(path, Encoding.UTF8);
            return Parse(reader);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read the language file \"{path}\". {ex.Message}");
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static Dictionary<string, string> Parse(TextReader reader)
    {
        Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.Ordinal);

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            int separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = trimmed.Substring(0, separator).Trim();
            string value = trimmed.Substring(separator + 1).Trim();
            if (key.Length > 0 && value.Length > 0)
            {
                entries[key] = value;
            }
        }

        return entries;
    }

    private static IEnumerable<KeyValuePair<string, string>> SortedEnglish()
    {
        List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>(StringTables.Fallback);
        entries.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
        return entries;
    }

    /// <summary><c>BepInEx/config/AdminMenu/Languages</c>, which survives mod updates.</summary>
    internal static string UserFolderPath()
    {
        try
        {
            return Path.Combine(BepInEx.Paths.ConfigPath, "AdminMenu", FolderName);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not find the BepInEx config folder. {ex.Message}");
            return null;
        }
    }

    /// <summary>Where earlier versions looked: beside the DLL.</summary>
    private static string LegacyFolderPath()
    {
        try
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            return string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, FolderName);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not work out where the mod is installed. {ex.Message}");
            return null;
        }
    }
}
