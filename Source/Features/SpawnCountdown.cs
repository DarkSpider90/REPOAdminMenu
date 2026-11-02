using System.Collections;
using AdminMenu.Actions;
using AdminMenu.Catalog;
using AdminMenu.Localization;
using UnityEngine;

namespace AdminMenu.Features;

/// <summary>
/// Counts down on screen before letting an enemy loose.
///
/// This is what replaces asking "are you sure" in a dialog. A dialog stops everything and
/// tells the host nothing useful; a countdown lets them close the menu, turn round and be
/// somewhere sensible before the thing arrives. It also makes a mistaken click survivable,
/// which is the only thing the dialog was really for.
///
/// Timed in unscaled seconds, because the menu is still open when the count starts.
/// </summary>
internal static class SpawnCountdown
{
    /// <summary>Seconds counted out loud before the spawn.</summary>
    private const int Seconds = 3;

    /// <summary>How long the "here it comes" line stays up afterwards.</summary>
    private const float DoneTime = 1.5f;

    private static Coroutine _running;

    /// <summary>
    /// Starts the count for one enemy. A second call replaces the first, so clicking twice
    /// does not queue two packs.
    /// </summary>
    internal static void Begin(CatalogEntry<EnemySetup> entry, int count)
    {
        if (entry == null || count <= 0)
        {
            return;
        }

        MonoBehaviour host = RunManager.instance;
        if (host == null)
        {
            // No one to run the count on, so the spawn happens straight away rather than not
            // at all.
            SpawnActions.SpawnEnemy(entry, count);
            return;
        }

        if (_running != null)
        {
            host.StopCoroutine(_running);
        }

        _running = host.StartCoroutine(Run(entry, count));
    }

    private static IEnumerator Run(CatalogEntry<EnemySetup> entry, int count)
    {
        string what = count > 1 ? $"{entry.DisplayName} x{count}" : entry.DisplayName;

        for (int remaining = Seconds; remaining > 0; remaining--)
        {
            ScreenNotice.Hold(Strings.Format(StringKeys.SpawnCountdown, what, remaining));
            yield return new WaitForSecondsRealtime(1f);
        }

        SpawnActions.SpawnEnemy(entry, count);

        ScreenNotice.Show(what, DoneTime);
        _running = null;
    }
}
