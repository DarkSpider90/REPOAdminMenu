using System;
using System.Collections.Generic;

namespace AdminMenu.Localization;

/// <summary>
/// The menu's own text, per language, keyed by the locale codes the game uses.
///
/// Item, enemy and upgrade names come from the game and are already translated; what is here
/// is the menu's own wording, which the game knows nothing about. English is complete and is
/// what every other language falls back to key by key, so a half-finished translation shows
/// translated text where it has it and English where it does not, never a blank button.
///
/// Every other language the game ships is a <c>.lang</c> file built into the mod (see
/// <see cref="LanguageFiles"/>) and merged in at startup; a file beside the mod can override
/// it or add a language without a rebuild.
/// </summary>
internal static class StringTables
{
    internal const string FallbackLocaleCode = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Tables =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StringKeys.MenuTitle] = "Admin Menu",
                [StringKeys.MenuShort] = "Admin",
                [StringKeys.MenuClose] = "Close",
                [StringKeys.MenuBack] = "Back",
                [StringKeys.MenuSearch] = "Search",
                [StringKeys.MenuHostOnly] = "Only the host can use this menu.",

                [StringKeys.TabPlayers] = "Players",
                [StringKeys.TabSpawn] = "Spawn",
                [StringKeys.TabLevel] = "Level",
                [StringKeys.TabOptions] = "Options",

                [StringKeys.PlayersTarget] = "Target",
                [StringKeys.PlayersFollowMe] = "Follow me",
                [StringKeys.PlayersUpgrades] = "Upgrades",
                [StringKeys.PlayersTeleport] = "Teleport",
                [StringKeys.PlayersDead] = "dead",
                [StringKeys.PlayersHost] = "host",

                [StringKeys.UpgradesLevel] = "Level",
                [StringKeys.UpgradesClearAll] = "Remove all upgrades",

                [StringKeys.TeleportPlayerToMe] = "Player to me",
                [StringKeys.TeleportPlayerTo] = "Send to",
                [StringKeys.TeleportGo] = "Teleport",
                [StringKeys.TeleportMeToPlayer] = "Me to player",
                [StringKeys.TeleportToTruck] = "To the truck",
                [StringKeys.TeleportToExtraction] = "To the extraction point",

                [StringKeys.PlayerGodMode] = "Immortality",
                [StringKeys.PlayerInvisible] = "Invisible to enemies",
                [StringKeys.PlayerInfiniteStamina] = "Infinite stamina",
                [StringKeys.PlayerFreeFlight] = "Free flight",
                [StringKeys.PlayerInfiniteAmmo] = "Infinite ammo",
                [StringKeys.PlayerOneShotEnemies] = "One-shot enemies",
                [StringKeys.PlayerHeal] = "Heal",
                [StringKeys.PlayerKill] = "Kill",
                [StringKeys.PlayerRevive] = "Revive",

                [StringKeys.SpawnItems] = "Shop items",
                [StringKeys.SpawnValuables] = "Valuables",
                [StringKeys.SpawnEnemies] = "Enemies",
                [StringKeys.SpawnCosmeticBoxes] = "Cosmetic boxes",
                [StringKeys.SpawnCount] = "Count",
                [StringKeys.SpawnTarget] = "Spawn for",
                [StringKeys.SpawnCountdown] = "Spawning {0} in {1}",

                [StringKeys.LevelSelect] = "Change level",
                [StringKeys.LevelCompleted] = "Levels completed",
                [StringKeys.LevelShop] = "Shop",
                [StringKeys.LevelMoney] = "Money",
                [StringKeys.LevelFreeItems] = "Free items",
                [StringKeys.LevelExtractionOpen] = "Activate extraction point",
                [StringKeys.LevelExtractionComplete] = "Complete extraction point",
                [StringKeys.LevelRevealValuables] = "Reveal all valuables",
                [StringKeys.LevelRevealBoxes] = "Reveal all cosmetic boxes",

                [StringKeys.RarityCommon] = "Common",
                [StringKeys.RarityUncommon] = "Uncommon",
                [StringKeys.RarityRare] = "Rare",
                [StringKeys.RarityUltraRare] = "Ultra rare",

                [StringKeys.SourceMod] = "mod",

                [StringKeys.CategoryLevels] = "Levels",
                [StringKeys.CategoryShops] = "Shops",
                [StringKeys.CategoryArenas] = "Arenas",
                [StringKeys.CategorySpecial] = "Special",
                [StringKeys.CategoryUpgrades] = "Upgrades",
                [StringKeys.CategoryCosmeticBoxes] = "Cosmetic boxes",
                [StringKeys.CategoryDifficulty] = "Difficulty {0}",

                [StringKeys.ItemTypePrefix + "drone"] = "Drones",
                [StringKeys.ItemTypePrefix + "orb"] = "Orbs",
                [StringKeys.ItemTypePrefix + "cart"] = "Carts",
                [StringKeys.ItemTypePrefix + "item_upgrade"] = "Item upgrades",
                [StringKeys.ItemTypePrefix + "player_upgrade"] = "Player upgrades",
                [StringKeys.ItemTypePrefix + "power_crystal"] = "Power crystals",
                [StringKeys.ItemTypePrefix + "grenade"] = "Grenades",
                [StringKeys.ItemTypePrefix + "melee"] = "Melee weapons",
                [StringKeys.ItemTypePrefix + "healthPack"] = "Health packs",
                [StringKeys.ItemTypePrefix + "gun"] = "Guns",
                [StringKeys.ItemTypePrefix + "tracker"] = "Trackers",
                [StringKeys.ItemTypePrefix + "mine"] = "Mines",
                [StringKeys.ItemTypePrefix + "pocket_cart"] = "Pocket carts",
                [StringKeys.ItemTypePrefix + "tool"] = "Tools",
                [StringKeys.ItemTypePrefix + "vehicle"] = "Vehicles",
                [StringKeys.ItemTypePrefix + "launcher"] = "Launchers",

                [StringKeys.ValuableSizePrefix + "Tiny"] = "Tiny",
                [StringKeys.ValuableSizePrefix + "Small"] = "Small",
                [StringKeys.ValuableSizePrefix + "Medium"] = "Medium",
                [StringKeys.ValuableSizePrefix + "Big"] = "Big",
                [StringKeys.ValuableSizePrefix + "Wide"] = "Wide",
                [StringKeys.ValuableSizePrefix + "Tall"] = "Tall",
                [StringKeys.ValuableSizePrefix + "VeryTall"] = "Very tall",

                [StringKeys.ValuableNameFormat] = "{1} {0}",
            },
        };

    /// <summary>The complete English table, which is what a template is written from.</summary>
    internal static IReadOnlyDictionary<string, string> Fallback => Tables[FallbackLocaleCode];

    /// <summary>Locale codes the mod has text for, built-in and loaded from file.</summary>
    internal static IEnumerable<string> LocaleCodes => Tables.Keys;

    internal static bool TryGet(string localeCode, string key, out string text)
    {
        text = null;
        return !string.IsNullOrEmpty(localeCode) &&
               Tables.TryGetValue(localeCode, out Dictionary<string, string> table) &&
               table.TryGetValue(key, out text) &&
               !string.IsNullOrEmpty(text);
    }

    /// <summary>
    /// Adds or overrides text for one language. A loaded file wins over the built-in table,
    /// so a translation can be corrected without waiting for a release.
    /// </summary>
    internal static void Merge(string localeCode, Dictionary<string, string> entries)
    {
        if (string.IsNullOrEmpty(localeCode) || entries == null || entries.Count == 0)
        {
            return;
        }

        if (!Tables.TryGetValue(localeCode, out Dictionary<string, string> table))
        {
            table = new Dictionary<string, string>(StringComparer.Ordinal);
            Tables[localeCode] = table;
        }

        foreach (KeyValuePair<string, string> pair in entries)
        {
            if (!string.IsNullOrEmpty(pair.Key) && !string.IsNullOrEmpty(pair.Value))
            {
                table[pair.Key] = pair.Value;
            }
        }
    }
}
