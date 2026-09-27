using System;
using UnityEngine;
using UnityEngine.UI;

public readonly struct ProjectMediaThumbModel
{
    public readonly int Index;
    public readonly ProjectMedia Media;
    public readonly bool IsSelected;
    public readonly Action<int> OnClick;
    public readonly VideoPosterCache PosterCache;

    public ProjectMediaThumbModel(int index, ProjectMedia media, bool isSelected, Action<int> onClick,
        VideoPosterCache posterCache)
    {
        Index = index;
        Media = media;
        IsSelected = isSelected;
        OnClick = onClick;
        PosterCache = posterCache;
    }
}

/// <summary>
/// One thumbnail in the detail view's Media strip. Videos without an image show their first frame
/// (captured once by the <see cref="VideoPosterCache"/>). Clicking shows the item in the hero.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ProjectMediaThumbView : MonoBehaviour, IUIItemView<ProjectMediaThumbModel>
{
    [SerializeField] private Image image;
    [Tooltip("Optional. Envelope Parent makes the image fill (and crop to) its masked frame.")]
    [SerializeField] private AspectRatioFitter imageFitter;
    [Tooltip("Shows a video's first frame when the media item has no image.")]
    [SerializeField] private RawImage posterImage;
    [SerializeField] private AspectRatioFitter posterFitter;
    [Tooltip("Shown on video items (e.g. a play badge).")]
    [SerializeField] private GameObject videoBadge;
    [Tooltip("Shown while this item is in the hero (e.g. a bright border).")]
    [SerializeField] private GameObject selectedState;

    private Button _button;
    private ProjectMediaThumbModel _model;
    private Action<VideoPoster> _applyPoster;

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

    public void Bind(ProjectMediaThumbModel model)
    {
        _model = model;
        ProjectMedia media = model.Media;
        UIBinding.SetSprite(image, media?.Image, imageFitter);
        UIBinding.SetActive(videoBadge, media != null && media.IsVideo);
        UIBinding.SetActive(selectedState, model.IsSelected);

        SetPoster(default);
        if (media != null && media.IsVideo && media.Image == null && model.PosterCache != null)
            RequestPoster(media, model.PosterCache);
    }

    private void RequestPoster(ProjectMedia media, VideoPosterCache cache)
    {
        string url = media.VideoUrl;
        if (cache.TryGet(url, out VideoPoster poster))
        {
            SetPoster(poster);
            return;
        }

        // Cached delegate; it checks the view still shows this media when the capture finishes.
        _applyPoster ??= ApplyRequestedPoster;
        cache.Request(url, _applyPoster);
    }

    private void ApplyRequestedPoster(VideoPoster poster)
    {
        ProjectMedia media = _model.Media;
        if (this != null && media != null && media.IsVideo && media.Image == null &&
            _model.PosterCache != null && _model.PosterCache.TryGet(media.VideoUrl, out VideoPoster current))
            SetPoster(current);
    }

    private void SetPoster(VideoPoster poster)
    {
        if (posterImage == null)
            return;

        posterImage.texture = poster.Texture;
        posterImage.enabled = poster.IsValid;
        if (poster.IsValid && posterFitter != null)
            posterFitter.aspectRatio = poster.AspectRatio;
    }

    private void HandleClick()
    {
        _model.OnClick?.Invoke(_model.Index);
    }
}
