using System.Collections.Generic;
using UnityEngine;

/// <summary>One project. Kept as its own asset so a future detail page can reuse the same data.</summary>
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

    public bool IsHidden => isHidden;
    public string Title => title;
    public string ShortDescription => shortDescription;
    public Sprite Thumbnail => thumbnail;
    public Color OutlineColor => outlineColor;
    public IReadOnlyList<ProjectCategory> Categories => categories;
    public IReadOnlyList<UILabelChip> Tags => tags;

    public bool IsInCategory(ProjectCategory category)
    {
        return category == null || categories.Contains(category);
    }
}
