using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace AdminMenu.Localization;

/// <summary>
/// Loads menu text for a language from a file beside the mod.
///
/// The game ships a long list of languages and this mod cannot be translated into all of them
/// by whoever builds it, so a language is a file rather than a code change:
/// <c>Languages/&lt;locale code&gt;.lang</c> next to AdminMenu.dll, one <c>key=value</c> per
/// line, <c>#</c> for a comment, UTF-8. The locale code is the one the game uses - "ru",
/// "pt-BR" - and a file's text overrides the built-in table for that language.
/// </summary>
internal static class LanguageFiles
{
    private const string FolderName = "Languages";
    private const string FileExtension = ".lang";

    /// <summary>
    /// Loads every language file next to the mod. Missing folder is the normal case and says
    /// nothing; a file that cannot be read is reported and skipped.
    /// </summary>
    internal static void LoadAll()
    {
        string folder = FolderPath();
        if (folder == null || !Directory.Exists(folder))
        {
            return;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(folder, "*" + FileExtension);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not list the language folder \"{folder}\". {ex.Message}");
            return;
        }

        foreach (string file in files)
        {
            string localeCode = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrEmpty(localeCode))
            {
                continue;
            }

            Dictionary<string, string> entries = Read(file);
            if (entries.Count > 0)
            {
                StringTables.Merge(localeCode, entries);
                Plugin.Log.LogInfo($"Loaded {entries.Count} menu strings for \"{localeCode}\".");
            }
        }
    }

    /// <summary>
    /// Writes the English table out as a template, so a translator has every key in front of
    /// them instead of having to read the source.
    /// </summary>
    internal static bool WriteTemplate()
    {
        string folder = FolderPath();
        if (folder == null)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "template" + FileExtension);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Admin Menu language template.");
            builder.AppendLine("# Copy to <locale code>.lang - \"ru.lang\", \"pt-BR.lang\" - and translate the right side.");
            builder.AppendLine("# Any key left out falls back to English.");
            builder.AppendLine();

            foreach (KeyValuePair<string, string> pair in EnglishEntries())
            {
                builder.Append(pair.Key).Append('=').AppendLine(pair.Value);
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            Plugin.Log.LogInfo($"Wrote the language template to \"{path}\".");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not write the language template. {ex.Message}");
            return false;
        }
    }

    private static Dictionary<string, string> Read(string path)
    {
        Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
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
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read the language file \"{path}\". {ex.Message}");
        }

        return entries;
    }

    private static IEnumerable<KeyValuePair<string, string>> EnglishEntries()
    {
        List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();

        foreach (FieldInfo field in typeof(StringKeys).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string) || field.GetValue(null) is not string key)
            {
                continue;
            }

            if (StringTables.TryGet(StringTables.FallbackLocaleCode, key, out string text))
            {
                entries.Add(new KeyValuePair<string, string>(key, text));
            }
        }

        entries.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
        return entries;
    }

    private static string FolderPath()
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
