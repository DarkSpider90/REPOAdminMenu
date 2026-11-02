using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace AdminMenu.Localization;

/// <summary>
/// Loads menu text for a language from a <c>.lang</c> file.
///
/// A language is a file rather than a code change: one <c>key=value</c> per line, <c>#</c>
/// for a comment, UTF-8, named after the locale code the game uses - "ru", "pt-BR". Every
/// language the game ships is built into the DLL this way (<c>Source/Localization/Languages</c>),
/// and a file in <c>Languages/</c> next to AdminMenu.dll is read afterwards, so it can correct
/// a built-in translation or add a language without a rebuild.
/// </summary>
internal static class LanguageFiles
{
    private const string FolderName = "Languages";
    private const string FileExtension = ".lang";

    /// <summary>Name prefix the project gives the built-in files; see AdminMenu.csproj.</summary>
    private const string ResourcePrefix = "AdminMenu.Languages.";

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

    private static IEnumerable<KeyValuePair<string, string>> EnglishEntries()
    {
        List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>(StringTables.Fallback);
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
