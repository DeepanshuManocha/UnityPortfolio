using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AboutItem
{
    [Tooltip("Keep the item in the data but don't show it in the About section.")]
    [SerializeField] private bool isHidden;
    [SerializeField] private string title;
    [SerializeField, TextArea(1, 3)] private string subtitle;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private Color outlineColor = Color.white;

    public bool IsHidden => isHidden;
    public string Title => title;
    public string Subtitle => subtitle;
    public Sprite Icon => icon;
    public Color IconColor => iconColor;
    public Color OutlineColor => outlineColor;
}

[CreateAssetMenu(
    fileName = "About Section Data",
    menuName = "Portfolio/UI/About Section Data")]
public class AboutSectionData : ScriptableObject
{
    [Header("Intro")]
    [SerializeField] private string eyebrow = "PORTFOLIO";
    [SerializeField] private string greeting = "Hello, I'm";
    [SerializeField] private string displayName;
    [SerializeField] private Gradient nameGradient = new();
    [SerializeField] private string role;
    [SerializeField] private Color roleColor = Color.white;
    [SerializeField, TextArea(3, 8)] private string bio;
    [Tooltip("Handwritten side note. Use new lines to stack words.")]
    [SerializeField, TextArea(1, 4)] private string tagline;
    [SerializeField] private Sprite background;

    [Header("Industries")]
    [SerializeField] private string industriesTitle = "Industries";
    [SerializeField] private Sprite industriesIcon;
    [SerializeField] private List<AboutItem> industries = new();

    [Header("Focus")]
    [SerializeField] private string focusTitle = "What I Focus On";
    [SerializeField] private Sprite focusIcon;
    [SerializeField] private List<AboutItem> focusItems = new();

    public string Eyebrow => eyebrow;
    public string Greeting => greeting;
    public string DisplayName => displayName;
    public Gradient NameGradient => nameGradient;
    public string Role => role;
    public Color RoleColor => roleColor;
    public string Bio => bio;
    public string Tagline => tagline;
    public Sprite Background => background;
    public string IndustriesTitle => industriesTitle;
    public Sprite IndustriesIcon => industriesIcon;
    public IReadOnlyList<AboutItem> Industries => industries;
    public string FocusTitle => focusTitle;
    public Sprite FocusIcon => focusIcon;
    public IReadOnlyList<AboutItem> FocusItems => focusItems;

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
