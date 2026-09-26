using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Projects Section Data",
    menuName = "Portfolio/Projects/Projects Section Data")]
public class ProjectsSectionData : ScriptableObject
{
    [Header("Header")]
    [SerializeField] private string titleLead = "All";
    [SerializeField] private string titleHighlight = "Projects";
    [SerializeField] private Gradient titleGradient = new();
    [SerializeField, TextArea(1, 3)] private string subtitle;

    [Header("Filters")]
    [SerializeField] private string allLabel = "All";
    [SerializeField] private Color allColor = new(0.23f, 0.51f, 0.96f, 1f);
    [Tooltip("{0} = category name, {1} = project count.")]
    [SerializeField] private string filterLabelFormat = "{0} ({1})";
    [Tooltip("Tab order. Projects reference these assets.")]
    [SerializeField] private List<ProjectCategory> categories = new();

    [Header("Projects")]
    [SerializeField, Min(1)] private int projectsPerPage = 4;
    [SerializeField] private string emptyMessage = "Nothing here yet. Check back soon.";
    [SerializeField] private List<ProjectData> projects = new();

    public string TitleLead => titleLead;
    public string TitleHighlight => titleHighlight;
    public Gradient TitleGradient => titleGradient;
    public string Subtitle => subtitle;
    public string AllLabel => allLabel;
    public Color AllColor => allColor;
    public string FilterLabelFormat => filterLabelFormat;
    public IReadOnlyList<ProjectCategory> Categories => categories;
    public int ProjectsPerPage => projectsPerPage;
    public string EmptyMessage => emptyMessage;
    public IReadOnlyList<ProjectData> Projects => projects;

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
