using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AdminMenu.Localization;
using HarmonyLib;
using MenuLib;
using MenuLib.MonoBehaviors;
using UnityEngine;

namespace AdminMenu.Menu;

/// <summary>
/// One page is visible at a time, and the way back is remembered.
///
/// MenuLib can stack pages on top of each other, which is wrong for a menu like this: the
/// page underneath stays drawn, its header prints through the new one, and its buttons are
/// still clickable. So a move closes the page it is leaving and opens the new one in its
/// place, and the builder that made the old page is kept on a stack so Back can draw it again
/// from current data rather than restoring a stale copy.
///
/// The small delay before opening is not decoration: closing a page runs an animation, and
/// opening the next one in the same frame leaves both half-drawn.
/// </summary>
internal static class MenuNavigator
{
    /// <summary>Long enough for the closing page to finish getting out of the way.</summary>
    private const float OpenDelay = 0.05f;

    private static readonly MethodInfo PageCloseAllMethod = AccessTools.Method(typeof(MenuManager), "PageCloseAll");

    private static readonly Stack<Func<REPOPopupPage>> History = new Stack<Func<REPOPopupPage>>();

    private static REPOPopupPage _current;
    private static Func<REPOPopupPage> _currentBuilder;

    /// <summary>
    /// A page is open, or about to be: the page is built before the short wait that lets the
    /// previous one finish closing, and a press during that wait still means "close".
    /// Comparing against null also covers a page the game destroyed from under us.
    /// </summary>
    internal static bool IsOpen => _current != null;

    /// <summary>Opens the first page, forgetting wherever the menu was last time.</summary>
    internal static void Start(Func<REPOPopupPage> builder)
    {
        History.Clear();
        Show(builder);
    }

    /// <summary>Goes to a new page, remembering the one being left.</summary>
    internal static void Go(Func<REPOPopupPage> builder)
    {
        if (_currentBuilder != null)
        {
            History.Push(_currentBuilder);
        }

        Show(builder);
    }

    /// <summary>
    /// Draws the current page again from current data. Used after a change the page itself
    /// prints - a level number, an amount of money - which a label will not update on its own.
    /// </summary>
    internal static void Refresh()
    {
        if (_currentBuilder != null)
        {
            Show(_currentBuilder);
        }
    }

    /// <summary>
    /// Puts a different page where the current one is without recording a step. Used when a
    /// page changes into a variant of itself, such as a list that has just been searched:
    /// Back should then leave the list, not undo the search.
    /// </summary>
    internal static void Replace(Func<REPOPopupPage> builder)
    {
        Show(builder);
    }

    internal static void Back()
    {
        if (History.Count == 0)
        {
            CloseAll();
            return;
        }

        Show(History.Pop());
    }

    internal static void CloseAll()
    {
        Close(_current);

        _current = null;
        _currentBuilder = null;
        History.Clear();
    }

    private static void Show(Func<REPOPopupPage> builder)
    {
        Close(_current);

        _currentBuilder = builder;

        REPOPopupPage page = builder();
        AddFooter(page);

        _current = page;

        // MenuManager is the only MonoBehaviour around that outlives the page being closed.
        MenuManager.instance.StartCoroutine(OpenWhenClear(page));
    }

    private static IEnumerator OpenWhenClear(REPOPopupPage page)
    {
        yield return new WaitForSeconds(OpenDelay);

        CloseEverything();
        page.OpenPage(openOnTop: false);
    }

    /// <summary>
    /// The footer sits on the page itself rather than in the scroll view, so it stays put
    /// while the contents scroll - the way the game's own pages are laid out.
    /// </summary>
    private static void AddFooter(REPOPopupPage page)
    {
        bool isRoot = History.Count == 0;
        string text = isRoot ? Strings.Get(StringKeys.MenuClose) : "← " + Strings.Get(StringKeys.MenuBack);
        Action onClick = isRoot ? (Action)CloseAll : Back;

        page.AddElement(parent => MenuAPI.CreateREPOButton(Strings.ForDisplay(text), onClick, parent, FooterPosition));
    }

    private static void Close(REPOPopupPage page)
    {
        if (page == null)
        {
            return;
        }

        CloseEverything();
        page.ClosePage(closePagesAddedOnTop: true);
        MenuManager.instance.PageRemove(page.menuPage);
    }

    /// <summary>
    /// Clears anything the game or another mod left open on top, so a page never opens behind
    /// something else.
    /// </summary>
    private static void CloseEverything()
    {
        if (PageCloseAllMethod == null || MenuManager.instance == null)
        {
            return;
        }

        try
        {
            PageCloseAllMethod.Invoke(MenuManager.instance, new object[0]);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not clear the open menu pages. {ex.Message}");
        }
    }

    /// <summary>Centred under the scroll area, where the game puts its own back buttons.</summary>
    private static readonly Vector2 FooterPosition = new Vector2(250f, 20f);
}
