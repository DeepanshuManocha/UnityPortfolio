using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct ProjectCardModel
{
    public readonly ProjectData Project;
    public readonly Action<ProjectData> OnSelected;

    public ProjectCardModel(ProjectData project, Action<ProjectData> onSelected)
    {
        Project = project;
        OnSelected = onSelected;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ProjectCardView : MonoBehaviour, IUIItemView<ProjectCardModel>
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image thumbnailImage;
    [Tooltip("Optional. Envelope Parent makes the thumbnail fill (and crop to) its masked frame at any aspect.")]
    [SerializeField] private AspectRatioFitter thumbnailFitter;
    [Tooltip("Borders tinted with the project's outline colour. Each keeps its authored alpha.")]
    [SerializeField] private Graphic[] outlineGraphics;
    [SerializeField] private UIItemViewList<UILabelChip, UILabelChipView> tags;

    private Button _button;
    private float[] _outlineBaseAlphas;
    private ProjectCardModel _model;

    public ProjectData Project => _model.Project;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(ProjectCardModel model)
    {
        _model = model;
        ProjectData project = model.Project;
        if (project == null)
            return;

        UIBinding.SetText(titleText, project.Title);
        UIBinding.SetText(descriptionText, project.ShortDescription);
        UIBinding.SetTints(outlineGraphics, ref _outlineBaseAlphas, project.OutlineColor);
        UIBinding.SetSprite(thumbnailImage, project.Thumbnail, thumbnailFitter);
        tags.Bind(project.Tags);
    }

    private void HandleClick()
    {
        if (_model.Project != null)
            _model.OnSelected?.Invoke(_model.Project);
    }
}
