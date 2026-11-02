using AdminMenu.Access;
using AdminMenu.Interop;
using AdminMenu.Targeting;
using UnityEngine;

namespace AdminMenu.Actions;

/// <summary>
/// What the menu can do to a player: heal, kill, revive, and the four teleports.
///
/// All of it goes through the game's own methods, which already send their own RPC and check
/// that the caller is the host - <c>HealOther</c>, <c>PlayerDeath</c>, <c>Revive</c>,
/// <c>Spawn</c>. Nothing here opens a channel of its own and nothing is patched.
///
/// A dead player is the one case that needs care: the body is switched off and the player
/// exists as a severed head with its own physics, so a teleport moves the head and the
/// ragdoll, which is exactly what the game's own revive does before it puts the body back.
/// </summary>
internal static class PlayerActions
{
    /// <summary>Heals the player to their current ceiling.</summary>
    internal static bool Heal(TargetPlayer player)
    {
        if (!Allowed() || !player.TryResolve(out PlayerAvatar avatar) || avatar.playerHealth == null)
        {
            return false;
        }

        int maxHealth = PlayerAvatarAccess.GetMaxHealth(avatar);
        if (maxHealth <= 0)
        {
            return false;
        }

        avatar.playerHealth.HealOther(maxHealth, effect: true);
        Plugin.LogVerbose($"Healed {player.Name} to {maxHealth}.");
        return true;
    }

    /// <summary>Kills the player. -1 says no enemy did it, which is how the game marks it.</summary>
    internal static bool Kill(TargetPlayer player)
    {
        if (!Allowed() || !player.TryResolve(out PlayerAvatar avatar))
        {
            return false;
        }

        if (PlayerAvatarAccess.IsDead(avatar))
        {
            return false;
        }

        avatar.PlayerDeath(-1);
        Plugin.LogVerbose($"Killed {player.Name}.");
        return true;
    }

    internal static bool Revive(TargetPlayer player)
    {
        if (!Allowed() || !player.TryResolve(out PlayerAvatar avatar))
        {
            return false;
        }

        if (!PlayerAvatarAccess.IsDead(avatar))
        {
            return false;
        }

        if (PlayerAvatarAccess.GetDeathHead(avatar) == null)
        {
            Plugin.Log.LogWarning($"{player.Name} has no death head, so they cannot be revived.");
            return false;
        }

        avatar.Revive(_revivedByTruck: false);
        Plugin.LogVerbose($"Revived {player.Name}.");
        return true;
    }

    /// <summary>Brings the player to the host. Their head, if either of them is dead.</summary>
    internal static bool TeleportToMe(TargetPlayer player)
    {
        return TeleportTo(player, TargetPlayerResolver.Local());
    }

    /// <summary>Takes the host to the player.</summary>
    internal static bool TeleportMeTo(TargetPlayer player)
    {
        return TeleportTo(TargetPlayerResolver.Local(), player);
    }

    /// <summary>
    /// Moves one player to another. Either of them may be dead: where a dead player is means
    /// where their head is, and moving a dead player means moving their head and ragdoll.
    /// </summary>
    internal static bool TeleportTo(TargetPlayer player, TargetPlayer destination)
    {
        return !player.Equals(destination) &&
               TargetPlayerResolver.TryGetPose(destination, out Vector3 position, out Quaternion rotation) &&
               Teleport(player, position, rotation);
    }

    internal static bool TeleportToTruck(TargetPlayer player)
    {
        TruckSafetySpawnPoint spawnPoint = TruckSafetySpawnPoint.instance;
        if (spawnPoint == null)
        {
            Plugin.Log.LogWarning("There is no truck spawn point in this level.");
            return false;
        }

        return Teleport(player, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }

    /// <summary>
    /// Sends the player to the extraction point that is running. Does nothing when none is
    /// active, because there is no single point to send them to before one opens.
    /// </summary>
    internal static bool TeleportToExtraction(TargetPlayer player)
    {
        ExtractionPoint point = ExtractionActions.CurrentPoint();
        if (point == null)
        {
            Plugin.Log.LogWarning("No extraction point is active, so there is nowhere to teleport to.");
            return false;
        }

        return Teleport(player, point.transform.position + Vector3.up, point.transform.rotation);
    }

    /// <summary>
    /// Moves a player. Alive, the game's own <c>Spawn</c> does it and covers singleplayer and
    /// multiplayer by itself. Dead, the head and the ragdoll are moved instead - the body
    /// object is switched off and the game puts it back at the head on revive.
    /// </summary>
    internal static bool Teleport(TargetPlayer player, Vector3 position, Quaternion rotation)
    {
        if (!Allowed() || !player.TryResolve(out PlayerAvatar avatar))
        {
            return false;
        }

        if (!PlayerAvatarAccess.IsDead(avatar))
        {
            avatar.Spawn(position, rotation, _debug: false);
            Plugin.LogVerbose($"Teleported {player.Name} to {position}.");
            return true;
        }

        bool moved = false;

        PhysGrabObject head = PlayerAvatarAccess.GetHeadPhysObject(avatar);
        if (head != null)
        {
            head.Teleport(position, rotation);
            moved = true;
        }

        PhysGrabObject tumble = PlayerAvatarAccess.GetTumblePhysObject(avatar);
        if (tumble != null)
        {
            tumble.Teleport(position, rotation);
            moved = true;
        }

        if (moved)
        {
            Plugin.LogVerbose($"Teleported the remains of {player.Name} to {position}.");
        }

        return moved;
    }

    private static bool Allowed()
    {
        if (AdminAccess.CanAct)
        {
            return true;
        }

        Plugin.Log.LogWarning("Only the host can change another player, so nothing was done.");
        return false;
    }
}
