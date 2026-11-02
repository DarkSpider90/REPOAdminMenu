using System.Collections.Generic;
using AdminMenu.Targeting;

namespace AdminMenu.Menu;

/// <summary>
/// The players as a list of names to step through with arrows, and a way back from the chosen
/// position to the player.
///
/// The arrows hand back a position, not a name, which is what this is for: two players with
/// the same name are two different answers, and a name on its own could not tell them apart.
/// </summary>
internal sealed class PlayerOptions
{
    private readonly List<TargetPlayer> _players;

    private PlayerOptions(List<TargetPlayer> players, string[] labels, int selectedIndex)
    {
        _players = players;
        Labels = labels;
        SelectedIndex = selectedIndex;
    }

    /// <summary>What the row shows, in the same order as the players.</summary>
    internal string[] Labels { get; }

    /// <summary>Where the row starts, which is whoever is chosen now.</summary>
    internal int SelectedIndex { get; }

    internal bool IsEmpty => _players.Count == 0;

    internal static PlayerOptions Build(TargetPlayer selected)
    {
        List<TargetPlayer> players = TargetPlayerService.ListPlayers();

        string[] labels = new string[players.Count];
        int index = 0;

        for (int i = 0; i < players.Count; i++)
        {
            labels[i] = PlayerLabel.For(players[i]);

            if (players[i].Equals(selected))
            {
                index = i;
            }
        }

        return new PlayerOptions(players, labels, index);
    }

    /// <summary>The player at this position, or nobody if the list changed underneath.</summary>
    internal TargetPlayer At(int index)
    {
        return index >= 0 && index < _players.Count ? _players[index] : TargetPlayer.None;
    }
}
