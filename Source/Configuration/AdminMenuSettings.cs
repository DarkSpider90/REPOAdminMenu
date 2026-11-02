using BepInEx.Configuration;
using UnityEngine;

namespace AdminMenu.Configuration;

/// <summary>
/// Every configurable value the mod has. Bound once from <see cref="Plugin.Awake"/>.
/// </summary>
internal static class AdminMenuSettings
{
    internal static ConfigEntry<KeyCode> MenuHotkey { get; private set; }
    internal static ConfigEntry<bool> EnableHotkey { get; private set; }
    internal static ConfigEntry<int> MaxUpgradeLevel { get; private set; }
    internal static ConfigEntry<bool> CloseMenuOnSpawn { get; private set; }
    internal static ConfigEntry<int> MenuFontSize { get; private set; }
    internal static ConfigEntry<bool> VerboseLogging { get; private set; }
    internal static ConfigEntry<bool> WriteLanguageTemplate { get; private set; }

    internal static void Bind(ConfigFile config)
    {
        EnableHotkey = config.Bind(
            "Menu", "EnableHotkey", true,
            "Open the admin menu with a key. Turn off to keep the key free for another mod.");

        MenuHotkey = config.Bind(
            "Menu", "MenuHotkey", KeyCode.F8,
            "Key that opens the admin menu.");

        CloseMenuOnSpawn = config.Bind(
            "Menu", "CloseMenuOnSpawn", false,
            "Close the menu after spawning something, instead of staying on the list.");

        MaxUpgradeLevel = config.Bind(
            "Upgrades", "MaxUpgradeLevel", 25,
            new ConfigDescription(
                "Highest level the upgrade sliders reach. The game itself has no cap; this only " +
                "bounds the menu.",
                new AcceptableValueRange<int>(1, 500)));

        MenuFontSize = config.Bind(
            "Menu", "MenuFontSize", 0,
            new ConfigDescription(
                "Size of the text in the menu. 0 leaves the sizing the game gives its own menu " +
                "rows, which is the default; set a number to force every label to that size.",
                new AcceptableValueRange<int>(0, 32)));

        VerboseLogging = config.Bind(
            "Diagnostics", "VerboseLogging", false,
            "Log catalog contents and every applied action. Useful in a bug report, noisy otherwise.");

        WriteLanguageTemplate = config.Bind(
            "Localization", "WriteLanguageTemplate", false,
            "Write every line of the menu, as it reads in the game's current language, to " +
            "BepInEx/config/AdminMenu/Languages/<language>.template.lang. Copy it to <language>.lang " +
            "there and edit it to change the menu's wording. Switches itself back off once written.");
    }
}
