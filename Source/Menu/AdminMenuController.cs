using AdminMenu.Access;

namespace AdminMenu.Menu;

/// <summary>
/// Opens and closes the admin menu, and is the only thing that decides whether it may open.
///
/// Two conditions, checked here rather than in every page: the run has to be in a level -
/// there is nothing to administer in the main menu - and this client has to be allowed to use
/// the menu.
///
/// Nothing is built here. The content lists are built by the pages that need them, when they
/// need them, so opening the menu costs one page and not a scan of every item, valuable and
/// enemy in the game.
/// </summary>
internal static class AdminMenuController
{
    internal static bool IsOpen => MenuNavigator.IsOpen;

    internal static void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    internal static void Open()
    {
        if (SemiFunc.MenuLevel())
        {
            Plugin.Log.LogInfo("The admin menu only opens during a run.");
            return;
        }

        if (!AdminAccess.CanOpenMenu)
        {
            Plugin.Log.LogInfo(AdminAccess.RefusalReason());
            return;
        }

        MenuNavigator.Start(RootPage.Build);
    }

    internal static void Close()
    {
        MenuNavigator.CloseAll();
    }
}
