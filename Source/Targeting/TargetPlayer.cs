using System;

namespace AdminMenu.Targeting;

/// <summary>
/// A player held by steam id rather than by object reference.
///
/// A <c>PlayerAvatar</c> is not a safe thing to remember: the object is replaced when a
/// player respawns and is destroyed when they leave, so a menu page that cached one would act
/// on a dead reference. The steam id is what the game's own networked calls take anyway, so
/// the menu remembers that and resolves the avatar at the moment of the action.
/// </summary>
internal readonly struct TargetPlayer : IEquatable<TargetPlayer>
{
    internal static readonly TargetPlayer None = default;

    private TargetPlayer(string steamId, string name)
    {
        SteamId = steamId;
        Name = name;
    }

    internal string SteamId { get; }

    /// <summary>Name as it was when the target was picked, for the menu's own labels.</summary>
    internal string Name { get; }

    internal bool IsValid => !string.IsNullOrEmpty(SteamId);

    internal static TargetPlayer From(PlayerAvatar avatar)
    {
        if (avatar == null)
        {
            return None;
        }

        string steamId = SemiFunc.PlayerGetSteamID(avatar);
        return string.IsNullOrEmpty(steamId)
            ? None
            : new TargetPlayer(steamId, SemiFunc.PlayerGetName(avatar) ?? steamId);
    }

    internal static TargetPlayer FromSteamId(string steamId, string name = null)
    {
        return string.IsNullOrEmpty(steamId) ? None : new TargetPlayer(steamId, name ?? steamId);
    }

    /// <summary>The avatar this id points at right now, or null if the player is gone.</summary>
    internal bool TryResolve(out PlayerAvatar avatar)
    {
        avatar = IsValid ? SemiFunc.PlayerAvatarGetFromSteamID(SteamId) : null;
        return avatar != null;
    }

    public bool Equals(TargetPlayer other)
    {
        return string.Equals(SteamId, other.SteamId, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is TargetPlayer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return SteamId == null ? 0 : StringComparer.Ordinal.GetHashCode(SteamId);
    }

    public override string ToString()
    {
        return IsValid ? $"{Name} [{SteamId}]" : "no target";
    }
}
