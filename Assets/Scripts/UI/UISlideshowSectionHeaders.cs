using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Highlights the header chosen for the slideshow's current image (each image in the slideshow's
/// <see cref="SlideshowImageCollection"/> picks one of its Headers): the selected header is full alpha and slightly larger,
/// the rest are dimmed. Uses CanvasGroup alpha and scale only, so layout groups are untouched.
/// </summary>
[DisallowMultipleComponent]
public class UISlideshowSectionHeaders : MonoBehaviour
{
    [SerializeField] private UIImageSlideshow slideshow;
    [Tooltip("One per header, in the same order as the collection's Headers.")]
    [SerializeField] private List<RectTransform> headers = new();

    [Header("Look")]
    [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float unselectedAlpha = 0.35f;
    [SerializeField, Min(0.5f)] private float selectedScale = 1.08f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.3f;
    [SerializeField] private bool useUnscaledTime;

    private CanvasGroup[] _groups;
    private Vector3[] _restingScales;
    private UnityAction<int, Sprite> _imageChanged;
    private Sequence _tween;
    private int _selectedSection = int.MinValue;

    public int SelectedSection => _selectedSection;

    private void Reset()
    {
        UseChildHeaders();
        slideshow = FindAnyObjectByType<UIImageSlideshow>();
    }

    private void Awake()
    {
        _imageChanged = HandleImageChanged;
        CacheHeaders();
    }

    private void OnEnable()
    {
        if (slideshow == null)
            return;

        slideshow.ImageChanged.AddListener(_imageChanged);
        SelectSectionForImage(slideshow.CurrentIndex, true);
    }

    private void OnDisable()
    {
        if (slideshow != null)
            slideshow.ImageChanged.RemoveListener(_imageChanged);
        _tween?.Kill();
        _tween = null;
    }

    [ContextMenu("Use Child Headers")]
    private void UseChildHeaders()
    {
        headers.Clear();
        foreach (Transform child in transform)
        {
            if (child is RectTransform rect)
                headers.Add(rect);
        }
    }

    /// <param name="section">Section index, or -1 when the image has no section (every header shown at full alpha).</param>
    public void SelectSection(int section, bool immediate = false)
    {
        if (section == _selectedSection)
            return;

        _selectedSection = section;
        _tween?.Kill();
        _tween = null;

        bool animate = !immediate && transitionDuration > 0f;
        if (animate)
            _tween = DOTween.Sequence().SetUpdate(useUnscaledTime).SetLink(gameObject);

        for (int i = 0; i < headers.Count; i++)
        {
            if (headers[i] == null)
                continue;

            bool isSelected = i == section;
            float alpha = section < 0 || isSelected ? selectedAlpha : unselectedAlpha;
            Vector3 scale = _restingScales[i] * (isSelected ? selectedScale : 1f);

            if (animate)
            {
                _tween.Join(_groups[i].DOFade(alpha, transitionDuration));
                _tween.Join(headers[i].DOScale(scale, transitionDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                _groups[i].alpha = alpha;
                headers[i].localScale = scale;
            }
        }
    }

    private void HandleImageChanged(int imageIndex, Sprite _)
    {
        SelectSectionForImage(imageIndex, false);
    }

    private void SelectSectionForImage(int imageIndex, bool immediate)
    {
        SlideshowImageCollection collection = slideshow.ImageCollection;
        int section = collection != null ? collection.GetHeaderIndex(imageIndex) : -1;
        SelectSection(section, immediate);
    }

    private void CacheHeaders()
    {
        _groups = new CanvasGroup[headers.Count];
        _restingScales = new Vector3[headers.Count];
        for (int i = 0; i < headers.Count; i++)
        {
            RectTransform header = headers[i];
            if (header == null)
                continue;

            if (!header.TryGetComponent(out _groups[i]))
                _groups[i] = header.gameObject.AddComponent<CanvasGroup>();
            _restingScales[i] = header.localScale;
        }
    }
}
