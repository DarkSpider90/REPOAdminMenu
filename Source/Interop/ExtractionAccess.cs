using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AdminMenu.Interop;

/// <summary>
/// Reaches the extraction point state the game keeps internal.
///
/// Opening a point is public API - <c>RoundDirector.RequestExtractionPointActivation</c> -
/// but finding a point that is still idle, and finishing the one that is running, are not:
/// the list of points, the active one, the point's state and its "finish right away" flag
/// are all internal or private. Completing a point goes through the game's own private
/// <c>StateSet</c> rather than its public RPC, because <c>StateSet</c> is what checks the
/// master client and sends the state to everyone.
/// </summary>
internal static class ExtractionAccess
{
    private static readonly FieldInfo PointActiveField =
        AccessTools.Field(typeof(RoundDirector), "extractionPointActive");
    private static readonly FieldInfo PointListField =
        AccessTools.Field(typeof(RoundDirector), "extractionPointList");
    private static readonly FieldInfo PointCurrentField =
        AccessTools.Field(typeof(RoundDirector), "extractionPointCurrent");

    private static readonly FieldInfo PointStateField =
        AccessTools.Field(typeof(ExtractionPoint), "currentState");
    private static readonly FieldInfo CompletedRightAwayField =
        AccessTools.Field(typeof(ExtractionPoint), "isCompletedRightAway");
    private static readonly MethodInfo StateSetMethod =
        AccessTools.Method(typeof(ExtractionPoint), "StateSet", new[] { typeof(ExtractionPoint.State) });

    private static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>True while one extraction point is running.</summary>
    internal static bool IsPointActive()
    {
        RoundDirector director = RoundDirector.instance;
        return director != null &&
               Read(PointActiveField, director, "RoundDirector.extractionPointActive") is bool active &&
               active;
    }

    /// <summary>The point that is running, or null when none is.</summary>
    internal static ExtractionPoint CurrentPoint()
    {
        RoundDirector director = RoundDirector.instance;
        return director == null
            ? null
            : Read(PointCurrentField, director, "RoundDirector.extractionPointCurrent") as ExtractionPoint;
    }

    /// <summary>Every extraction point in the level, in the order the game registered them.</summary>
    internal static List<ExtractionPoint> AllPoints()
    {
        List<ExtractionPoint> points = new List<ExtractionPoint>();

        RoundDirector director = RoundDirector.instance;
        if (director == null)
        {
            return points;
        }

        if (Read(PointListField, director, "RoundDirector.extractionPointList") is not IEnumerable<GameObject> list)
        {
            return points;
        }

        foreach (GameObject gameObject in list)
        {
            ExtractionPoint point = gameObject?.GetComponent<ExtractionPoint>();
            if (point != null)
            {
                points.Add(point);
            }
        }

        return points;
    }

    internal static bool TryGetState(ExtractionPoint point, out ExtractionPoint.State state)
    {
        state = ExtractionPoint.State.None;

        if (point == null)
        {
            return false;
        }

        object value = Read(PointStateField, point, "ExtractionPoint.currentState");
        if (value is not ExtractionPoint.State read)
        {
            return false;
        }

        state = read;
        return true;
    }

    /// <summary>
    /// Tells the point to finish without waiting for the haul to be met. The game reads this
    /// flag to skip its own haul check.
    /// </summary>
    internal static bool SetCompletedRightAway(ExtractionPoint point, bool value)
    {
        if (point == null || CompletedRightAwayField == null)
        {
            ReportMissing("ExtractionPoint.isCompletedRightAway");
            return false;
        }

        try
        {
            CompletedRightAwayField.SetValue(point, value);
            return true;
        }
        catch (Exception ex)
        {
            ReportMissing("ExtractionPoint.isCompletedRightAway", ex);
            return false;
        }
    }

    /// <summary>
    /// Moves the point to a state through the game's own setter, which checks the master
    /// client and sends the change to every player.
    /// </summary>
    internal static bool SetState(ExtractionPoint point, ExtractionPoint.State state)
    {
        if (point == null || StateSetMethod == null)
        {
            ReportMissing("ExtractionPoint.StateSet");
            return false;
        }

        try
        {
            StateSetMethod.Invoke(point, new object[] { state });
            return true;
        }
        catch (Exception ex)
        {
            ReportMissing("ExtractionPoint.StateSet", ex);
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

    private static void ReportMissing(string name, Exception ex = null)
    {
        if (!Reported.Add(name))
        {
            return;
        }

        Plugin.Log.LogWarning(
            $"{name} could not be reached, so the extraction point controls will not work. " +
            (ex == null ? string.Empty : $"{ex.GetType().Name}: {ex.Message}"));
    }
}
