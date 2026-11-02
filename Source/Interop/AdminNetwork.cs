using System;
using AdminMenu.Features;
using ExitGames.Client.Photon;

namespace AdminMenu.Interop;

/// <summary>
/// Carries a granted ability from the host to the player it was granted to.
///
/// Some of what the menu offers cannot be done to someone from the outside. Stamina, and where
/// a body is, are worked out on that player's own machine and sent to everyone else, so the
/// host pushing them around would be undone on the next frame. The way round it is not to push
/// at all: the host says "this player may fly now" and that player's own copy of the mod turns
/// it on for them.
///
/// A player without the mod simply never hears it. The message is a Photon event with a code
/// nothing else is listening for, so it is read by the mod or by nobody - there is no error and
/// nothing to crash. The host sees the switch stay on, which is honest: permission was given,
/// and whether anyone acted on it is not something the host can know.
///
/// REPOLib owns the event code, so without REPOLib this works for the host alone.
/// </summary>
internal static class AdminNetwork
{
    private const string EventName = "AdminMenu Player Flag";

    /// <summary>Held as object so that a missing REPOLib is never a type that fails to load.</summary>
    private static object _event;

    private static bool _tried;

    internal static bool Available => Event() != null;

    /// <summary>Registers the channel. Called once the game is up, not from Awake.</summary>
    internal static void Initialize()
    {
        Event();
    }

    /// <summary>
    /// Tells everyone that a player may, or may no longer, use an ability. Sent to all rather
    /// than to one, because Photon addresses clients by actor number and what we have is a
    /// steam id; every client reads the message and only the named one acts on it.
    /// </summary>
    internal static void Send(string steamId, AdminFlag flag, bool state)
    {
        if (string.IsNullOrEmpty(steamId))
        {
            return;
        }

        object channel = Event();
        if (channel == null)
        {
            return;
        }

        try
        {
            Raise(channel, new object[] { steamId, (byte)flag, state });
            Plugin.LogVerbose($"Sent {flag}={state} for {steamId}.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not send {flag} for {steamId}. {ex.Message}");
        }
    }

    private static void Raise(object channel, object content)
    {
        REPOLib.Modules.NetworkedEvent typed = (REPOLib.Modules.NetworkedEvent)channel;
        typed.RaiseEvent(content, REPOLib.Modules.NetworkingEvents.RaiseAll, SendOptions.SendReliable);
    }

    private static object Event()
    {
        if (_tried)
        {
            return _event;
        }

        _tried = true;

        if (!RepoLibBridge.Available)
        {
            Plugin.Log.LogWarning(
                "REPOLib is not installed, so abilities granted to other players cannot be sent to them.");
            return null;
        }

        try
        {
            _event = new REPOLib.Modules.NetworkedEvent(EventName, Handle);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not open the admin channel. {ex.Message}");
            _event = null;
        }

        return _event;
    }

    private static void Handle(EventData data)
    {
        try
        {
            if (data.CustomData is not object[] parts || parts.Length < 3)
            {
                return;
            }

            string steamId = parts[0] as string;
            if (string.IsNullOrEmpty(steamId) || !IsLocal(steamId))
            {
                return;
            }

            AdminFlags.ApplyLocally((AdminFlag)Convert.ToByte(parts[1]), Convert.ToBoolean(parts[2]));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not read an admin message. {ex.Message}");
        }
    }

    private static bool IsLocal(string steamId)
    {
        PlayerAvatar local = SemiFunc.PlayerAvatarLocal();
        return local != null && string.Equals(SemiFunc.PlayerGetSteamID(local), steamId, StringComparison.Ordinal);
    }
}
