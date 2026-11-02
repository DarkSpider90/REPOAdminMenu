using System.Collections.Generic;
using AdminMenu.Localization;
using AdminMenu.Targeting;

namespace AdminMenu.Menu;

/// <summary>
/// How a player is written in a list.
///
/// Every list of players in the menu reads the same way, which is the point of having this in
/// one place: the host is marked, the dead are marked, and a player who is both says so once
/// rather than twice over.
/// </summary>
internal static class PlayerLabel
{
    internal static string For(TargetPlayer player)
    {
        List<string> tags = new List<string>(2);

        if (TargetPlayerResolver.IsHost(player))
        {
            tags.Add(Strings.Get(StringKeys.PlayersHost));
        }

        if (TargetPlayerResolver.IsDead(player))
        {
            tags.Add(Strings.Get(StringKeys.PlayersDead));
        }

        return tags.Count == 0 ? player.Name : $"{player.Name} ({string.Join(", ", tags)})";
    }
}
