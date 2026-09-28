using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuickLinkType
{
    OpenUrl,
    SendEmail,
    DownloadFile,
    Custom
}

[Serializable]
public class QuickLink
{
    [SerializeField] private bool isHidden;
    [Tooltip("Shown when the button expands on hover.")]
    [SerializeField] private string label;
    [SerializeField] private Sprite icon;
    [SerializeField] private QuickLinkType type;
    [Tooltip("OpenUrl / DownloadFile: a URL or a file path inside StreamingAssets (e.g. Resume.pdf).\n" +
             "SendEmail: the email address.\n" +
             "Custom: the id of a Custom Action on the QuickLinksView (e.g. restart-tour).")]
    [SerializeField] private string value;
    [Tooltip("SendEmail only.")]
    [SerializeField] private string emailSubject;
    [Tooltip("Draws a short divider above this button.")]
    [SerializeField] private bool separatorBefore;
    [Tooltip("Invisible and unclickable (but keeps its space) while the onboarding tour runs.")]
    [SerializeField] private bool hideDuringTour;

    public bool IsHidden => isHidden;
    public string Label => label;
    public Sprite Icon => icon;
    public QuickLinkType Type => type;
    public string Value => value;
    public bool SeparatorBefore => separatorBefore;
    public bool HideDuringTour => hideDuringTour;
    public bool IsWebLink => type != QuickLinkType.Custom;

    public WebLinkMode Mode => type switch
    {
        QuickLinkType.SendEmail => WebLinkMode.SameWindow,
        QuickLinkType.DownloadFile => WebLinkMode.Download,
        _ => WebLinkMode.NewTab
    };

    /// <summary>Full URL: mailto: for emails, StreamingAssets for relative paths, otherwise the value as-is.</summary>
    public string ResolveUrl()
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (type == QuickLinkType.SendEmail)
        {
            return string.IsNullOrEmpty(emailSubject)
                ? "mailto:" + value
                : "mailto:" + value + "?subject=" + Uri.EscapeDataString(emailSubject);
        }

        return value.Contains("://") ? value : Application.streamingAssetsPath + "/" + value.TrimStart('/');
    }
}

[CreateAssetMenu(
    fileName = "Quick Links",
    menuName = "Portfolio/UI/Quick Links")]
public class QuickLinksData : ScriptableObject
{
    [Tooltip("Top to bottom.")]
    [SerializeField] private List<QuickLink> links = new();

    public IReadOnlyList<QuickLink> Links => links;

#if UNITY_EDITOR
    /// <summary>Editor only: raised after inspector edits so bound views can refresh in Play Mode.</summary>
    public event Action Changed;

    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= RaiseChanged;
        UnityEditor.EditorApplication.delayCall += RaiseChanged;
    }

    private void RaiseChanged()
    {
        if (this != null)
            Changed?.Invoke();
    }
#endif
}
