namespace AdminMenu.Localization;

/// <summary>
/// Every piece of text the menu shows, by key. Kept apart from the tables so a missing
/// translation is a lookup that falls back, never a typo nobody notices.
/// </summary>
internal static class StringKeys
{
    internal const string MenuTitle = "menu.title";
    internal const string MenuShort = "menu.short";
    internal const string MenuClose = "menu.close";
    internal const string MenuBack = "menu.back";
    internal const string MenuSearch = "menu.search";
    internal const string MenuHostOnly = "menu.host_only";

    internal const string TabPlayers = "tab.players";
    internal const string TabSpawn = "tab.spawn";
    internal const string TabLevel = "tab.level";
    internal const string TabOptions = "tab.options";

    internal const string PlayersTarget = "players.target";
    internal const string PlayersFollowMe = "players.follow_me";
    internal const string PlayersUpgrades = "players.upgrades";
    internal const string PlayersTeleport = "players.teleport";
    internal const string PlayersDead = "players.dead";
    internal const string PlayersHost = "players.host";

    internal const string UpgradesLevel = "upgrades.level";
    internal const string UpgradesClearAll = "upgrades.clear_all";

    internal const string TeleportPlayerToMe = "teleport.player_to_me";
    internal const string TeleportPlayerTo = "teleport.player_to";
    internal const string TeleportGo = "teleport.go";
    internal const string TeleportMeToPlayer = "teleport.me_to_player";
    internal const string TeleportToTruck = "teleport.to_truck";
    internal const string TeleportToExtraction = "teleport.to_extraction";

    internal const string PlayerGodMode = "player.god_mode";
    internal const string PlayerInvisible = "player.invisible";
    internal const string PlayerInfiniteStamina = "player.infinite_stamina";
    internal const string PlayerFreeFlight = "player.free_flight";
    internal const string PlayerInfiniteAmmo = "player.infinite_ammo";
    internal const string PlayerOneShotEnemies = "player.one_shot_enemies";
    internal const string PlayerHeal = "player.heal";
    internal const string PlayerKill = "player.kill";
    internal const string PlayerRevive = "player.revive";

    internal const string SpawnItems = "spawn.items";
    internal const string SpawnValuables = "spawn.valuables";
    internal const string SpawnEnemies = "spawn.enemies";
    internal const string SpawnCosmeticBoxes = "spawn.cosmetic_boxes";
    internal const string SpawnCount = "spawn.count";
    internal const string SpawnTarget = "spawn.target";
    internal const string SpawnCountdown = "spawn.countdown";

    internal const string LevelSelect = "level.select";
    internal const string LevelCompleted = "level.completed";
    internal const string LevelShop = "level.shop";
    internal const string LevelMoney = "level.money";
    internal const string LevelFreeItems = "level.free_items";
    internal const string LevelExtractionOpen = "level.extraction_open";
    internal const string LevelExtractionComplete = "level.extraction_complete";
    internal const string LevelRevealValuables = "level.reveal_valuables";
    internal const string LevelRevealBoxes = "level.reveal_boxes";

    internal const string RarityCommon = "rarity.common";
    internal const string RarityUncommon = "rarity.uncommon";
    internal const string RarityRare = "rarity.rare";
    internal const string RarityUltraRare = "rarity.ultra_rare";

    internal const string SourceMod = "source.mod";

    internal const string CategoryLevels = "category.levels";
    internal const string CategoryShops = "category.shops";
    internal const string CategoryArenas = "category.arenas";
    internal const string CategorySpecial = "category.special";
    internal const string CategoryUpgrades = "category.upgrades";
    internal const string CategoryCosmeticBoxes = "category.cosmetic_boxes";
    internal const string CategoryDifficulty = "category.difficulty";

    // Shop item kinds, one per SemiFunc.itemType; the suffix is the enum name.
    internal const string ItemTypePrefix = "item_type.";

    // Valuable sizes, one per ValuableVolume.Type; the suffix is the enum name.
    internal const string ValuableSizePrefix = "valuable_size.";

    // Names the game itself never translated. The suffix is the prefab name without its
    // kind ("Manor Vase" for "Valuable Manor Vase", "Lobby" for "Level - Lobby").
    internal const string ValuableNamePrefix = "valuable.";
    internal const string ValuableThemePrefix = "valuable_theme.";
    internal const string LevelNamePrefix = "level_name.";

    // How a valuable and the level it belongs to are put together: {0} the thing, {1} the place.
    internal const string ValuableNameFormat = "valuable_format";
}
