# Changelog

## Unreleased

- The menu is translated into all 21 languages the game ships and switches with the game's language setting.
- Category names (item kinds, valuable sizes, enemy difficulty, level groups) are translated too; they were English in every language.
- Arabic is joined and laid out right to left, with player names and numbers kept readable.
- Valuables, which the game names only in English, get adapted names in every language, with the level they come from ("Ваза (Поместье)"). Levels the game leaves unnamed - lobby, arenas, tutorial, the shops - are named too.
- Your own wording fixes now live in `BepInEx/config/AdminMenu/Languages`, so a mod update no longer deletes them. The language template is written in your game's language with the current translation, and lines with a typo in the key or a broken `{0}` are skipped with a note in the log.

## 0.8.0

- First release. A host-only menu on F8 for players, spawning and the run.
- Players: upgrades, immortality, invisibility to enemies, infinite stamina, free flight, heal, kill, revive, and five ways to teleport. Dead players work everywhere the living do.
- Spawning: shop items, valuables, enemies and cosmetic boxes, with search and a choice of who they land next to. Enemies arrive on a countdown.
- The run: change the level, set how many levels are done, set the money, make the shop free, open or finish the extraction point, reveal every valuable and cosmetic box.
- Content from other mods appears in the lists beside the game's own.
- English and Russian, with other languages addable from a file.
