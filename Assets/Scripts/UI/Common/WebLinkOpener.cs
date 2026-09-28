using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public enum WebLinkMode
{
    NewTab,
    SameWindow,
    Download
}

/// <summary>
/// Opens URLs in a way browsers accept. On WebGL, <see cref="ArmOnPointerDown"/> lets the page open the link
/// on pointer release (inside the real click), which pop-up blockers allow; everywhere else it opens immediately.
/// </summary>
public static class WebLinkOpener
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void PortfolioOpenLinkOnRelease(string url, string fileName, int mode);
#endif

    /// <summary>Call from OnPointerDown. Returns true when the browser will open the link itself on release.</summary>
    public static bool ArmOnPointerDown(string url, WebLinkMode mode)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (string.IsNullOrEmpty(url))
            return false;

        PortfolioOpenLinkOnRelease(url, GetFileName(url), (int)mode);
        return true;
#else
        return false;
#endif
    }

    /// <summary>Opens straight away (editor, desktop, or keyboard/controller submit).</summary>
    public static void Open(string url, WebLinkMode mode)
    {
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);
    }

    private static string GetFileName(string url)
    {
        int queryStart = url.IndexOf('?');
        string path = queryStart >= 0 ? url.Substring(0, queryStart) : url;
        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path.Substring(slash + 1) : path;
    }
}
