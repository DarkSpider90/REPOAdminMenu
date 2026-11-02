using System;
using System.Reflection;
using HarmonyLib;

namespace AdminMenu.Interop;

/// <summary>
/// Reads the parts of a player the game keeps to itself.
///
/// Most of what the menu needs is public or has a <c>SemiFunc</c> helper - the steam id, the
/// name, the player list - and those are called directly. What is left is marked internal to
/// the game assembly: whether a player is dead, their severed head, and who the spectate
/// camera is following. Each accessor returns a harmless value if a game update renames its
/// field, so a rename costs that one feature rather than the menu.
/// </summary>
internal static class PlayerAvatarAccess
{
    private static readonly FieldInfo DeadSetField = AccessTools.Field(typeof(PlayerAvatar), "deadSet");
    private static readonly FieldInfo IsDisabledField = AccessTools.Field(typeof(PlayerAvatar), "isDisabled");
    private static readonly FieldInfo SpectatingField = AccessTools.Field(typeof(PlayerAvatar), "spectating");
    private static readonly FieldInfo DeathHeadField = AccessTools.Field(typeof(PlayerAvatar), "playerDeathHead");
    private static readonly FieldInfo SpectatedPlayerField = AccessTools.Field(typeof(SpectateCamera), "player");
    private static readonly FieldInfo TumbleField = AccessTools.Field(typeof(PlayerAvatar), "tumble");
    private static readonly FieldInfo TumblePhysObjectField = AccessTools.Field(typeof(PlayerTumble), "physGrabObject");
    private static readonly FieldInfo HeadPhysObjectField = AccessTools.Field(typeof(PlayerDeathHead), "physGrabObject");
    private static readonly FieldInfo MaxHealthField = AccessTools.Field(typeof(PlayerHealth), "maxHealth");
    private static readonly FieldInfo HealthField = AccessTools.Field(typeof(PlayerHealth), "health");
    private static readonly FieldInfo GodModeField = AccessTools.Field(typeof(PlayerHealth), "godMode");

    /// <summary>True when the player is down and only their head is left.</summary>
    internal static bool IsDead(PlayerAvatar player)
    {
        return player != null && ReadBool(DeadSetField, player, "PlayerAvatar.deadSet");
    }

    /// <summary>True while the game has the player switched off, between rounds for instance.</summary>
    internal static bool IsDisabled(PlayerAvatar player)
    {
        return player != null && ReadBool(IsDisabledField, player, "PlayerAvatar.isDisabled");
    }

    internal static bool IsSpectating(PlayerAvatar player)
    {
        return player != null && ReadBool(SpectatingField, player, "PlayerAvatar.spectating");
    }

    /// <summary>
    /// The player's severed head, which is a separate object with its own physics: a dead
    /// player is only moved if the head moves with them.
    /// </summary>
    internal static PlayerDeathHead GetDeathHead(PlayerAvatar player)
    {
        if (player == null || DeathHeadField == null)
        {
            ReportMissing("PlayerAvatar.playerDeathHead");
            return null;
        }

        try
        {
            return DeathHeadField.GetValue(player) as PlayerDeathHead;
        }
        catch (Exception ex)
        {
            ReportMissing("PlayerAvatar.playerDeathHead", ex);
            return null;
        }
    }

    /// <summary>
    /// The player the spectate camera is following, or null when nobody is being spectated.
    /// This is how an action aimed at "me" keeps working after the host dies.
    /// </summary>
    internal static PlayerAvatar GetSpectatedPlayer()
    {
        SpectateCamera camera = SpectateCamera.instance;
        if (camera == null)
        {
            return null;
        }

        if (SpectatedPlayerField == null)
        {
            ReportMissing("SpectateCamera.player");
            return null;
        }

        try
        {
            return SpectatedPlayerField.GetValue(camera) as PlayerAvatar;
        }
        catch (Exception ex)
        {
            ReportMissing("SpectateCamera.player", ex);
            return null;
        }
    }

    /// <summary>
    /// The physics object of the player's head. Teleporting a dead player means teleporting
    /// this, because the game's own revive puts the body back at wherever the head is.
    /// </summary>
    internal static PhysGrabObject GetHeadPhysObject(PlayerAvatar player)
    {
        PlayerDeathHead head = GetDeathHead(player);
        return head == null
            ? null
            : Read(HeadPhysObjectField, head, "PlayerDeathHead.physGrabObject") as PhysGrabObject;
    }

    /// <summary>
    /// The physics object of the player's ragdoll. The game teleports this alongside the head
    /// when it revives someone, so the menu moves both as well.
    /// </summary>
    internal static PhysGrabObject GetTumblePhysObject(PlayerAvatar player)
    {
        if (player == null)
        {
            return null;
        }

        object tumble = Read(TumbleField, player, "PlayerAvatar.tumble");
        return tumble == null
            ? null
            : Read(TumblePhysObjectField, tumble, "PlayerTumble.physGrabObject") as PhysGrabObject;
    }

    /// <summary>The player's health ceiling, which is what "heal to full" means.</summary>
    internal static int GetMaxHealth(PlayerAvatar player)
    {
        if (player?.playerHealth == null)
        {
            return 0;
        }

        return Read(MaxHealthField, player.playerHealth, "PlayerHealth.maxHealth") is int value ? value : 0;
    }

    internal static int GetHealth(PlayerAvatar player)
    {
        if (player?.playerHealth == null)
        {
            return 0;
        }

        return Read(HealthField, player.playerHealth, "PlayerHealth.health") is int value ? value : 0;
    }

    /// <summary>
    /// The game's own immortality flag, which <c>PlayerHealth.Hurt</c> checks before doing
    /// anything. Writing it needs no patch - but it only has an effect on the client that owns
    /// the player, because that is the only client where <c>Hurt</c> runs.
    /// </summary>
    internal static bool GetGodMode(PlayerAvatar player)
    {
        if (player?.playerHealth == null)
        {
            return false;
        }

        return Read(GodModeField, player.playerHealth, "PlayerHealth.godMode") is bool value && value;
    }

    internal static bool SetGodMode(PlayerAvatar player, bool value)
    {
        if (player?.playerHealth == null || GodModeField == null)
        {
            ReportMissing("PlayerHealth.godMode");
            return false;
        }

        try
        {
            GodModeField.SetValue(player.playerHealth, value);
            return true;
        }
        catch (Exception ex)
        {
            ReportMissing("PlayerHealth.godMode", ex);
            return false;
        }
    }

    private static object Read(FieldInfo field, object instance, string name)
    {
        if (field == null)
        {
            ReportMissing(name);
            return null;
        }

        try
        {
            return field.GetValue(instance);
        }
        catch (Exception ex)
        {
            ReportMissing(name, ex);
            return null;
        }
    }

    private static bool ReadBool(FieldInfo field, object instance, string name)
    {
        if (field == null)
        {
            ReportMissing(name);
            return false;
        }

        try
        {
            return (bool)field.GetValue(instance);
        }
        catch (Exception ex)
        {
            ReportMissing(name, ex);
            return false;
        }
    }

    private static readonly System.Collections.Generic.HashSet<string> Reported =
        new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

    private static void ReportMissing(string name, Exception ex = null)
    {
        if (!Reported.Add(name))
        {
            return;
        }

        Plugin.Log.LogWarning(
            $"{name} could not be reached, so the menu will treat it as unset. " +
            (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
