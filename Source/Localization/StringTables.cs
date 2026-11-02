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
/// A language that is not here can be added without rebuilding the mod - see
/// <see cref="LanguageFiles"/> - which is how the remaining languages the game ships are
/// meant to arrive.
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
                [StringKeys.LevelExtractionOpen] = "Open extraction point",
                [StringKeys.LevelExtractionComplete] = "Complete extraction point",
                [StringKeys.LevelRevealValuables] = "Reveal all valuables",
                [StringKeys.LevelRevealBoxes] = "Reveal all cosmetic boxes",

                [StringKeys.RarityCommon] = "Common",
                [StringKeys.RarityUncommon] = "Uncommon",
                [StringKeys.RarityRare] = "Rare",
                [StringKeys.RarityUltraRare] = "Ultra rare",

                [StringKeys.SourceMod] = "mod",
            },

            ["ru"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [StringKeys.MenuTitle] = "Меню админа",
                [StringKeys.MenuShort] = "Админ",
                [StringKeys.MenuClose] = "Закрыть",
                [StringKeys.MenuBack] = "Назад",
                [StringKeys.MenuSearch] = "Поиск",
                [StringKeys.MenuHostOnly] = "Меню доступно только хосту.",

                [StringKeys.TabPlayers] = "Игроки",
                [StringKeys.TabSpawn] = "Спавн",
                [StringKeys.TabLevel] = "Уровень",
                [StringKeys.TabOptions] = "Опции",

                [StringKeys.PlayersTarget] = "Цель",
                [StringKeys.PlayersFollowMe] = "За мной",
                [StringKeys.PlayersUpgrades] = "Улучшения",
                [StringKeys.PlayersTeleport] = "Телепорт",
                [StringKeys.PlayersDead] = "мёртв",
                [StringKeys.PlayersHost] = "хост",

                [StringKeys.UpgradesLevel] = "Уровень",
                [StringKeys.UpgradesClearAll] = "Снять все улучшения",

                [StringKeys.TeleportPlayerToMe] = "Игрока ко мне",
                [StringKeys.TeleportPlayerTo] = "Отправить к",
                [StringKeys.TeleportGo] = "Телепортировать",
                [StringKeys.TeleportMeToPlayer] = "Меня к игроку",
                [StringKeys.TeleportToTruck] = "К фургону",
                [StringKeys.TeleportToExtraction] = "К точке вывоза",

                [StringKeys.PlayerGodMode] = "Бессмертие",
                [StringKeys.PlayerInvisible] = "Невидимость для врагов",
                [StringKeys.PlayerInfiniteStamina] = "Бесконечная выносливость",
                [StringKeys.PlayerFreeFlight] = "Свободный полёт",
                [StringKeys.PlayerInfiniteAmmo] = "Бесконечные патроны",
                [StringKeys.PlayerOneShotEnemies] = "Враги с одного удара",
                [StringKeys.PlayerHeal] = "Вылечить",
                [StringKeys.PlayerKill] = "Убить",
                [StringKeys.PlayerRevive] = "Оживить",

                [StringKeys.SpawnItems] = "Предметы магазина",
                [StringKeys.SpawnValuables] = "Ценности",
                [StringKeys.SpawnEnemies] = "Враги",
                [StringKeys.SpawnCosmeticBoxes] = "Косметик-боксы",
                [StringKeys.SpawnCount] = "Количество",
                [StringKeys.SpawnTarget] = "Спавн для",
                [StringKeys.SpawnCountdown] = "Спавн {0} через {1}",

                [StringKeys.LevelSelect] = "Сменить уровень",
                [StringKeys.LevelCompleted] = "Пройдено уровней",
                [StringKeys.LevelShop] = "Магазин",
                [StringKeys.LevelMoney] = "Деньги",
                [StringKeys.LevelFreeItems] = "Бесплатные предметы",
                [StringKeys.LevelExtractionOpen] = "Открыть точку вывоза",
                [StringKeys.LevelExtractionComplete] = "Завершить точку вывоза",
                [StringKeys.LevelRevealValuables] = "Показать все ценности",
                [StringKeys.LevelRevealBoxes] = "Показать все косметик-боксы",

                [StringKeys.RarityCommon] = "Обычный",
                [StringKeys.RarityUncommon] = "Необычный",
                [StringKeys.RarityRare] = "Редкий",
                [StringKeys.RarityUltraRare] = "Очень редкий",

                [StringKeys.SourceMod] = "мод",
            },
        };

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
