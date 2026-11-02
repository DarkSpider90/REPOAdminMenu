using System;

namespace AdminMenu.Localization;

/// <summary>
/// Notices when the player changes the game's language.
///
/// Item, enemy and upgrade names are resolved once when a catalog is built, so a language
/// change has to invalidate them; <see cref="Version"/> counts the changes and the catalogs
/// treat a new number as a reason to rebuild. The subscription is made the first time the
/// game's localization manager exists, because the mod loads before it does.
/// </summary>
internal static class LocaleWatcher
{
    private static Action _callback;
    private static bool _subscribed;

    /// <summary>Goes up by one on every language change.</summary>
    internal static int Version { get; private set; }

    /// <summary>
    /// Subscribes if the game is ready for it. Cheap to call repeatedly, which is how it is
    /// called: from the catalog's staleness check rather than from a patch.
    /// </summary>
    internal static void EnsureSubscribed()
    {
        if (_subscribed || LocalizationManager.instance == null)
        {
            return;
        }

        try
        {
            _callback = OnLocalizationChanged;
            LocalizationManager.instance.AddLocalizationChangedCallback(_callback);
            _subscribed = true;
        }
        catch (Exception ex)
        {
            // Not fatal: names simply keep the language they were read in until the next
            // level change rebuilds them anyway.
            Plugin.Log.LogWarning($"Could not subscribe to language changes, so menu names may lag behind. {ex.Message}");
            _callback = null;
            _subscribed = true;
        }
    }

    internal static void Unsubscribe()
    {
        if (!_subscribed || _callback == null || LocalizationManager.instance == null)
        {
            return;
        }

        try
        {
            LocalizationManager.instance.RemoveLocalizationChangedCallback(_callback);
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not unsubscribe from language changes. {ex.Message}");
        }

        _callback = null;
        _subscribed = false;
    }

    /// <summary>Name of the language the game is in, for the menu's own footer.</summary>
    internal static string ActiveLocaleName()
    {
        LocaleAsset locale = Strings.ActiveLocaleAsset();
        return locale == null ? Strings.ActiveLocaleCode() : locale.NativeName ?? string.Empty;
    }

    private static void OnLocalizationChanged()
    {
        Version++;
        Plugin.LogVerbose($"Language changed to \"{ActiveLocaleName()}\"; menu names will be rebuilt.");
    }
}
