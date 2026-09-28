using System;
using UnityEngine;

/// <summary>
/// Counts open modal UIs (e.g. the project detail view) so overlays like the onboarding tour can step aside
/// without referencing each modal. Modals call <see cref="NotifyOpened"/> / <see cref="NotifyClosed"/> in pairs.
/// </summary>
public static class UIModalTracker
{
    public static int OpenCount { get; private set; }
    public static bool IsAnyOpen => OpenCount > 0;

    /// <summary>Raised when the first modal opens (true) and when the last one closes (false).</summary>
    public static event Action<bool> AnyOpenChanged;

    public static void NotifyOpened()
    {
        OpenCount++;
        if (OpenCount == 1)
            AnyOpenChanged?.Invoke(true);
    }

    public static void NotifyClosed()
    {
        if (OpenCount == 0)
            return;

        OpenCount--;
        if (OpenCount == 0)
            AnyOpenChanged?.Invoke(false);
    }

    // Keeps the count correct when Enter Play Mode runs without a domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        OpenCount = 0;
        AnyOpenChanged = null;
    }
}
