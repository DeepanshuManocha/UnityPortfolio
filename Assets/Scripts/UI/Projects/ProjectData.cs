using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProjectMedia
{
    [Tooltip("The picture, or the poster frame for a video.")]
    [SerializeField] private Sprite image;
    [Tooltip("Optional. A video file inside StreamingAssets (e.g. \"Projects/rsn-trailer.mp4\") or a full URL.")]
    [SerializeField] private string videoPath;

    public Sprite Image => image;
    public string VideoPath => videoPath;
    public bool IsVideo => !string.IsNullOrEmpty(videoPath);

    /// <summary>Playable URL: a full URL as-is, otherwise the path inside StreamingAssets.</summary>
    public string VideoUrl => !IsVideo || videoPath.Contains("://")
        ? videoPath
        : Application.streamingAssetsPath + "/" + videoPath.TrimStart('/');
}

/// <summary>One project: the card in the Projects section and the content of its detail view.</summary>
[CreateAssetMenu(
    fileName = "Project",
    menuName = "Portfolio/Projects/Project")]
public class ProjectData : ScriptableObject
{
    [Tooltip("Keep the project in the data but don't show it in the Projects section.")]
    [SerializeField] private bool isHidden;
    [SerializeField] private string title;
    [SerializeField, TextArea(1, 3)] private string shortDescription;
    [SerializeField] private Sprite thumbnail;
    [SerializeField] private Color outlineColor = new(0.098f, 0.251f, 0.4f, 0.698f);

    [Header("Filtering")]
    [SerializeField] private List<ProjectCategory> categories = new();

    [Header("Tags")]
    [SerializeField] private List<UILabelChip> tags = new();

    [Header("Details")]
    [SerializeField, TextArea(3, 8)] private string about;
    [SerializeField, TextArea(3, 8)] private string role;
    [Tooltip("The first item is the large hero. The Media strip only appears when there's more than one.")]
    [SerializeField] private List<ProjectMedia> media = new();
    [SerializeField] private List<TechStackItem> techStack = new();

    public bool IsHidden => isHidden;
    public string Title => title;
    public string ShortDescription => shortDescription;
    public Sprite Thumbnail => thumbnail;
    public Color OutlineColor => outlineColor;
    public IReadOnlyList<ProjectCategory> Categories => categories;
    public IReadOnlyList<UILabelChip> Tags => tags;
    public string About => about;
    public string Role => role;
    public IReadOnlyList<ProjectMedia> Media => media;
    public IReadOnlyList<TechStackItem> TechStack => techStack;

    public bool IsInCategory(ProjectCategory category)
    {
        return category == null || categories.Contains(category);
    }
}
