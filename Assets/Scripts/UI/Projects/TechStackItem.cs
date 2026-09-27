using UnityEngine;

/// <summary>A tool or technology (Unity, C#, Blender...). One asset, referenced by every project that uses it.</summary>
[CreateAssetMenu(
    fileName = "Tech Stack Item",
    menuName = "Portfolio/Projects/Tech Stack Item")]
public class TechStackItem : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public Sprite Icon => icon;
}
