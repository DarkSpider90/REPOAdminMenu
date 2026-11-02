using System.Collections.Generic;
using AdminMenu.Access;
using AdminMenu.Interop;

namespace AdminMenu.Actions;

/// <summary>
/// Opens the next extraction point and finishes the one that is running.
///
/// Opening presses the point's own button through <c>ExtractionPoint.OnClick</c>, so the game
/// decides whether it is allowed and tells every client. Finishing sets the point's own
/// "finish right away" flag and then moves it to Extracting through the game's state setter,
/// which is what the point does when the haul is met.
/// </summary>
internal static class ExtractionActions
{
    internal static ExtractionPoint CurrentPoint()
    {
        return ExtractionAccess.CurrentPoint();
    }

    internal static bool IsActive()
    {
        return ExtractionAccess.IsPointActive();
    }

    /// <summary>
    /// Opens an extraction point that has not been used yet. Does nothing while one is
    /// already running, which is the game's own rule.
    /// </summary>
    internal static bool OpenNext()
    {
        if (!Allowed())
        {
            return false;
        }

        if (ExtractionAccess.IsPointActive())
        {
            Plugin.Log.LogWarning("An extraction point is already running.");
            return false;
        }

        if (RoundDirector.instance == null)
        {
            return false;
        }

        List<ExtractionPoint> points = ExtractionAccess.AllPoints();
        if (points.Count == 0)
        {
            Plugin.Log.LogWarning("This level has no extraction points.");
            return false;
        }

        foreach (ExtractionPoint point in points)
        {
            // State is read only to skip points that are plainly finished. OnClick checks it
            // again itself, so a reflection failure here costs nothing.
            if (ExtractionAccess.TryGetState(point, out ExtractionPoint.State state) &&
                state != ExtractionPoint.State.Idle)
            {
                continue;
            }

            // The same call the button in the level makes. It is the only entry point that
            // covers all three cases: a client asking the host, the host activating directly,
            // and singleplayer, where there is no network to carry a request at all. Going
            // through RoundDirector.RequestExtractionPointActivation instead, as this did at
            // first, sends an RPC that never arrives in a single-player run.
            point.OnClick();

            if (ExtractionAccess.IsPointActive())
            {
                Plugin.LogVerbose($"Opened extraction point {point.name}.");
                return true;
            }
        }

        Plugin.Log.LogWarning("No extraction point could be opened; every one of them is already used.");
        return false;
    }

    /// <summary>Finishes the running extraction point without meeting the haul goal.</summary>
    internal static bool CompleteCurrent()
    {
        if (!Allowed())
        {
            return false;
        }

        ExtractionPoint point = ExtractionAccess.CurrentPoint();
        if (point == null || !ExtractionAccess.IsPointActive())
        {
            Plugin.Log.LogWarning("No extraction point is running, so there is nothing to complete.");
            return false;
        }

        ExtractionAccess.SetCompletedRightAway(point, true);

        if (!ExtractionAccess.SetState(point, ExtractionPoint.State.Extracting))
        {
            ExtractionAccess.SetCompletedRightAway(point, false);
            return false;
        }

        Plugin.LogVerbose($"Completed extraction point {point.name}.");
        return true;
    }

    private static bool Allowed()
    {
        if (AdminAccess.CanAct)
        {
            return true;
        }

        Plugin.Log.LogWarning("Only the host can control the extraction points.");
        return false;
    }
}
