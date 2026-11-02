using AdminMenu.Configuration;
using AdminMenu.Interop;
using AdminMenu.Menu;
using UnityEngine;

namespace AdminMenu.Features;

/// <summary>
/// The handful of things the mod has to do every frame, in one place.
///
/// Opening the admin channel lives here because it has to happen on every client, not just the
/// host: a player is only told they may fly if their own copy of the mod is listening, and a
/// client never sends anything of its own to make that happen.
///
/// Blocking movement lives here because the game does not stop listening to the keyboard while
/// a menu page is open - walk away with the menu up and you walk away for real. The game's own
/// call switches input off for one frame, so it is made again each frame the menu is open.
///
/// Hurrying a freshly spawned enemy lives here for the same reason: it is a value that has to
/// be held down across a few frames, and it costs nothing on the frames when nothing is
/// waiting.
/// </summary>
internal static class AdminTick
{
    private static bool _wasHotkeyDown;

    internal static void Run()
    {
        AdminNetwork.Initialize();
        SpawnedEnemyGuard.Tick();

        if (AdminMenuController.IsOpen)
        {
            SemiFunc.InputDisableMovement();
            SemiFunc.InputDisableAiming();
        }

        PollHotkey();
    }

    /// <summary>
    /// Opens the menu on the key. Caught on the edge, so holding the key does not open and
    /// close it once per frame.
    /// </summary>
    private static void PollHotkey()
    {
        if (!AdminMenuSettings.EnableHotkey.Value)
        {
            return;
        }

        bool isDown = Input.GetKey(AdminMenuSettings.MenuHotkey.Value);
        if (isDown && !_wasHotkeyDown)
        {
            AdminMenuController.Toggle();
        }

        _wasHotkeyDown = isDown;
    }
}
