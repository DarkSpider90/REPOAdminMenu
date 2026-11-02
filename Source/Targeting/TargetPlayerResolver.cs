using System.Collections.Generic;
using AdminMenu.Interop;
using Photon.Pun;
using UnityEngine;

namespace AdminMenu.Targeting;

/// <summary>
/// Answers the questions the menu asks about a target: who is in the lobby, who counts as
/// "me", where the target is, and where something spawned for them should land.
///
/// The interesting case is a dead host. Once the host dies the game hands them a spectate
/// camera, and from then on "me" is whoever that camera is following - otherwise every spawn
/// would land at a corpse the host is no longer looking at. A dead target is located by their
/// severed head, which is a separate object with its own physics.
/// </summary>
internal static class TargetPlayerResolver
{
    /// <summary>Lifts a spawn clear of the floor and of the player's own collider.</summary>
    private const float SpawnUpOffset = 0.6f;
    private const float SpawnForwardOffset = 0.8f;

    /// <summary>
    /// Everyone in the lobby, dead included, with the host first.
    ///
    /// The host is the one using the menu and the one most of its actions are relative to, so
    /// they belong at the top rather than wherever the game happens to have put them. The rest
    /// keep the game's own order.
    /// </summary>
    internal static List<TargetPlayer> ListPlayers()
    {
        List<TargetPlayer> players = new List<TargetPlayer>();

        List<PlayerAvatar> avatars = SemiFunc.PlayerGetAll();
        if (avatars == null)
        {
            return players;
        }

        foreach (PlayerAvatar avatar in avatars)
        {
            TargetPlayer player = TargetPlayer.From(avatar);
            if (player.IsValid && !players.Contains(player))
            {
                players.Add(player);
            }
        }

        TargetPlayer host = Host();
        int index = host.IsValid ? players.IndexOf(host) : -1;
        if (index > 0)
        {
            players.RemoveAt(index);
            players.Insert(0, host);
        }

        return players;
    }

    /// <summary>
    /// Whoever is running the lobby. In a single-player run that is the only player there is.
    /// </summary>
    internal static TargetPlayer Host()
    {
        if (!SemiFunc.IsMultiplayer())
        {
            return Local();
        }

        return PhotonNetwork.MasterClient == null
            ? TargetPlayer.None
            : TargetPlayer.From(SemiFunc.PlayerAvatarGetFromPhotonPlayer(PhotonNetwork.MasterClient));
    }

    internal static bool IsHost(TargetPlayer player)
    {
        TargetPlayer host = Host();
        return host.IsValid && host.Equals(player);
    }

    /// <summary>
    /// The player at this machine, alive or dead. This is what "me" means in an action: the
    /// host teleporting someone to themselves means to themselves, not to whichever corpse
    /// their camera happens to be following.
    /// </summary>
    internal static TargetPlayer Local()
    {
        return TargetPlayer.From(SemiFunc.PlayerAvatarLocal());
    }

    /// <summary>
    /// The local player, or the player they are spectating once they are dead. Used only
    /// where the question is "where is the host looking" - what a spawn falls back to when no
    /// one has been picked - and never for an action aimed at the host themselves.
    /// </summary>
    internal static TargetPlayer LocalOrSpectated()
    {
        PlayerAvatar local = SemiFunc.PlayerAvatarLocal();

        if (local != null && !PlayerAvatarAccess.IsDead(local))
        {
            return TargetPlayer.From(local);
        }

        PlayerAvatar spectated = PlayerAvatarAccess.GetSpectatedPlayer();
        if (spectated != null && spectated != local)
        {
            return TargetPlayer.From(spectated);
        }

        return TargetPlayer.From(local);
    }

    internal static bool IsLocal(TargetPlayer player)
    {
        PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
        return local != null && player.Equals(TargetPlayer.From(local));
    }

    internal static bool IsDead(TargetPlayer player)
    {
        return player.TryResolve(out PlayerAvatar avatar) && PlayerAvatarAccess.IsDead(avatar);
    }

    /// <summary>
    /// Where the target is: their own transform while alive, their head once dead. False when
    /// the player has left or the game has them switched off.
    /// </summary>
    internal static bool TryGetPose(TargetPlayer player, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (!player.TryResolve(out PlayerAvatar avatar))
        {
            return false;
        }

        if (PlayerAvatarAccess.IsDead(avatar))
        {
            PlayerDeathHead head = PlayerAvatarAccess.GetDeathHead(avatar);
            if (head != null)
            {
                position = head.transform.position;
                rotation = head.transform.rotation;
                return true;
            }
        }

        position = avatar.transform.position;
        rotation = avatar.transform.rotation;
        return true;
    }

    /// <summary>
    /// Where to put something spawned for this target: just in front of them and off the
    /// floor, so it does not land inside their collider or clip through the ground.
    /// </summary>
    internal static bool TryGetSpawnPoint(TargetPlayer player, out Vector3 position)
    {
        position = Vector3.zero;

        if (!TryGetPose(player, out Vector3 pose, out Quaternion rotation))
        {
            // The target is unreachable, so fall back to whatever the local camera is
            // observing - which is where the host is looking, spectating included.
            return TryGetObservedPosition(out position);
        }

        position = pose + Vector3.up * SpawnUpOffset + rotation * Vector3.forward * SpawnForwardOffset;
        return true;
    }

    /// <summary>The point the local camera is observing, which follows a spectated player.</summary>
    internal static bool TryGetObservedPosition(out Vector3 position)
    {
        position = Vector3.zero;

        try
        {
            if (AudioListenerFollow.instance == null || AudioListenerFollow.instance.TargetPositionTransform == null)
            {
                return false;
            }

            position = SemiFunc.PlayerGetObservedPosition() + Vector3.up * SpawnUpOffset;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
