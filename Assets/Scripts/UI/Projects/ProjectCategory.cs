using UnityEngine;

[CreateAssetMenu(
    fileName = "Project Category",
    menuName = "Portfolio/Projects/Project Category")]
public class ProjectCategory : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Color color = Color.white;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public Color Color => color;
}
