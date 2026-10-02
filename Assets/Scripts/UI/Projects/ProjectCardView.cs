using System;
using System.Collections.Generic;
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

/// <summary>
/// One project card. With a thumbnail it uses the authored layout; without one the thumbnail frame is hidden
/// and the info block takes the whole card: larger title at the top, bigger tags pinned to the bottom, and the
/// description wrapping into whatever space is left between them (cut with "..." if it doesn't fit).
/// </summary>
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

    [Header("No Thumbnail Layout")]
    [Tooltip("Hidden when the project has no thumbnail.")]
    [SerializeField] private GameObject thumbnailFrame;
    [Tooltip("Stretched over the whole card (minus the inset) when there's no thumbnail.")]
    [SerializeField] private RectTransform info;
    [SerializeField, Min(0f)] private float noThumbnailInset = 28f;
    [SerializeField, Min(1f)] private float noThumbnailTextScale = 1.3f;
    [Tooltip("Preferred tag size without a thumbnail. Tags shrink below this (and below 1 with a thumbnail) " +
             "when they wouldn't fit on one row.")]
    [SerializeField, Min(0.1f)] private float noThumbnailTagScale = 1.15f;

    private Button _button;
    private float[] _outlineBaseAlphas;
    private ProjectCardModel _model;

    // Authored ("with thumbnail") layout, captured once before the first switch.
    private bool _hasRestingLayout;
    private bool? _isCompact;
    private UIRectState _infoRest;
    private VerticalLayoutGroup _infoLayout;
    private TextAnchor _infoAlignmentRest;
    private float _descriptionFlexibleRest;
    private TextOverflowModes _descriptionOverflowRest;
    private LayoutElement _titleElement;
    private LayoutElement _descriptionElement;
    private float _titleSizeRest;
    private float _descriptionSizeRest;
    private float _titleHeightRest;
    private float _descriptionHeightRest;
    private TextWrappingModes _descriptionWrapRest;
    private float _tagRowHeightRest;

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

        bool hasThumbnail = project.Thumbnail != null;
        UIBinding.SetText(titleText, project.Title);
        UIBinding.SetText(descriptionText, project.ShortDescription);
        UIBinding.SetTints(outlineGraphics, ref _outlineBaseAlphas, project.OutlineColor);
        UIBinding.SetSprite(thumbnailImage, project.Thumbnail, thumbnailFitter);
        UIBinding.SetActive(GetThumbnailFrame(), hasThumbnail);
        ApplyLayout(!hasThumbnail);

        tags.Bind(project.Tags);
        ScaleTags(hasThumbnail ? 1f : noThumbnailTagScale);
    }

    private void ApplyLayout(bool isCompact)
    {
        if (_isCompact == isCompact)
            return;

        CaptureRestingLayout();
        _isCompact = isCompact;
        float textScale = isCompact ? noThumbnailTextScale : 1f;

        if (info != null)
        {
            if (isCompact)
            {
                // Full card height, keeping the authored left/right insets.
                new UIRectState(new Vector2(_infoRest.AnchorMin.x, 0f), new Vector2(_infoRest.AnchorMax.x, 1f),
                    new Vector2(_infoRest.Pivot.x, 0.5f),
                    new Vector2(_infoRest.OffsetMin.x, noThumbnailInset),
                    new Vector2(_infoRest.OffsetMax.x, -noThumbnailInset)).ApplyTo(info);
            }
            else
            {
                _infoRest.ApplyTo(info);
            }
        }

        if (_infoLayout != null)
            _infoLayout.childAlignment = isCompact ? TextAnchor.UpperLeft : _infoAlignmentRest;

        if (titleText != null)
        {
            titleText.fontSize = _titleSizeRest * textScale;
            SetHeight(_titleElement, _titleHeightRest * textScale);
        }

        if (descriptionText != null)
        {
            descriptionText.fontSize = _descriptionSizeRest * textScale;
            descriptionText.textWrappingMode = isCompact ? TextWrappingModes.Normal : _descriptionWrapRest;
            descriptionText.overflowMode = isCompact ? TextOverflowModes.Ellipsis : _descriptionOverflowRest;
            SetHeight(_descriptionElement, _descriptionHeightRest * textScale);
            if (_descriptionElement != null)
            {
                // Compact: the description soaks up the free space, so the tags always sit at the bottom
                // of the info block and nothing can push them out of the card.
                _descriptionElement.flexibleHeight = isCompact ? 1f : _descriptionFlexibleRest;
            }
        }

    }

    private void ScaleTags(float requestedScale)
    {
        float scale = Mathf.Min(requestedScale, GetTagFitScale());
        IReadOnlyList<UILabelChipView> chips = tags.Views;
        for (int i = 0; i < chips.Count; i++)
        {
            if (chips[i] != null && chips[i].gameObject.activeSelf)
                chips[i].SetScale(scale);
        }

        if (tags.Container != null && tags.Container.TryGetComponent(out LayoutElement rowElement))
        {
            CaptureRestingLayout();
            SetHeight(rowElement, _tagRowHeightRest * scale);
        }
    }

    /// <summary>Largest chip scale at which all active tags fit side by side in the info block's width.</summary>
    private float GetTagFitScale()
    {
        if (info == null || tags.Container == null)
            return float.MaxValue;

        float available = info.rect.width;
        if (_infoLayout != null)
            available -= _infoLayout.padding.horizontal;

        float widthAtScaleOne = 0f;
        int count = 0;
        IReadOnlyList<UILabelChipView> chips = tags.Views;
        for (int i = 0; i < chips.Count; i++)
        {
            if (chips[i] == null || !chips[i].gameObject.activeSelf)
                continue;

            widthAtScaleOne += chips[i].GetPreferredWidth(1f);
            count++;
        }

        if (count == 0 || widthAtScaleOne <= 0f)
            return float.MaxValue;

        if (tags.Container.TryGetComponent(out HorizontalLayoutGroup row))
            available -= row.spacing * (count - 1) + row.padding.horizontal;

        return available > 0f ? available / widthAtScaleOne : 1f;
    }

    private void CaptureRestingLayout()
    {
        if (_hasRestingLayout)
            return;

        _hasRestingLayout = true;
        if (info == null && titleText != null)
            info = titleText.transform.parent as RectTransform;
        if (info != null)
        {
            _infoRest = UIRectState.Capture(info);
            if (info.TryGetComponent(out _infoLayout))
                _infoAlignmentRest = _infoLayout.childAlignment;
        }

        if (titleText != null)
        {
            _titleSizeRest = titleText.fontSize;
            titleText.TryGetComponent(out _titleElement);
            _titleHeightRest = _titleElement != null ? _titleElement.preferredHeight : 0f;
        }

        if (descriptionText != null)
        {
            _descriptionSizeRest = descriptionText.fontSize;
            _descriptionWrapRest = descriptionText.textWrappingMode;
            _descriptionOverflowRest = descriptionText.overflowMode;
            descriptionText.TryGetComponent(out _descriptionElement);
            _descriptionHeightRest = _descriptionElement != null ? _descriptionElement.preferredHeight : 0f;
            _descriptionFlexibleRest = _descriptionElement != null ? _descriptionElement.flexibleHeight : -1f;
        }

        if (tags.Container != null && tags.Container.TryGetComponent(out LayoutElement rowElement))
            _tagRowHeightRest = rowElement.preferredHeight;
    }

    /// <summary>The assigned frame, else the thumbnail's parent (unless that's the card itself).</summary>
    private GameObject GetThumbnailFrame()
    {
        if (thumbnailFrame == null && thumbnailImage != null)
        {
            Transform parent = thumbnailImage.transform.parent;
            if (parent != null && parent != transform)
                thumbnailFrame = parent.gameObject;
        }

        return thumbnailFrame;
    }

    private static void SetHeight(LayoutElement element, float height)
    {
        if (element == null || height <= 0f)
            return;

        element.minHeight = height;
        element.preferredHeight = height;
    }

    private void HandleClick()
    {
        if (_model.Project != null)
            _model.OnSelected?.Invoke(_model.Project);
    }
}
