using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum SlideshowDirection
{
    Left,
    Right,
    Up,
    Down
}

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UIImageSlideshow : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private SlideshowImageCollection imageCollection;
    [SerializeField, Min(0)] private int startIndex;

    [Header("Scene References")]
    [Tooltip("The image displayed when the slideshow starts.")]
    [SerializeField] private Image primaryImage;
    [Tooltip("The second image is reused as the incoming slide.")]
    [SerializeField] private Image bufferImage;
    [Tooltip("Its width or height defines the slide distance. Usually the masked parent of both images.")]
    [SerializeField] private RectTransform viewport;

    [Header("Automation")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField, Min(0f)] private float imageDuration = 3f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.5f;
    [SerializeField] private bool useUnscaledTime;

    [Header("Transition")]
    [SerializeField] private SlideshowDirection direction = SlideshowDirection.Left;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent<int, Sprite> imageChanged;

    private Image _currentImage;
    private Image _nextImage;
    private Vector2 _restingPosition;
    private Vector2 _travelOffset;
    private int _currentIndex = -1;
    private int _targetIndex = -1;
    private float _displayTimer;
    private float _transitionTimer;
    private bool _isInitialized;
    private bool _isPlaying;
    private bool _isTransitioning;

    public int CurrentIndex => _currentIndex;
    public Sprite CurrentSprite => _currentImage != null ? _currentImage.sprite : null;
    public bool IsPlaying => _isPlaying;
    public bool IsTransitioning => _isTransitioning;

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Reset()
    {
        viewport = transform as RectTransform;
    }

    private void OnEnable()
    {
        _isPlaying = playOnEnable;
        Initialize();
    }

    private void OnDisable()
    {
        _isTransitioning = false;

        if (!_isInitialized)
            return;

        primaryImage.rectTransform.anchoredPosition = _restingPosition;
        bufferImage.rectTransform.anchoredPosition = _restingPosition;
        primaryImage.enabled = true;
        bufferImage.enabled = false;
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        if (_isTransitioning)
        {
            UpdateTransition();
            return;
        }

        if (!_isPlaying)
            return;

        _displayTimer += DeltaTime;
        if (_displayTimer >= imageDuration)
            Next();
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public bool Next()
    {
        return TryStartAdjacentTransition(1);
    }

    public bool Previous()
    {
        return TryStartAdjacentTransition(-1);
    }

    public bool Show(int index)
    {
        if (!_isInitialized || _isTransitioning || !HasImage(index) || index == _currentIndex)
            return false;

        BeginTransition(index, 1);
        return true;
    }

    public bool ShowImmediately(int index)
    {
        if (!_isInitialized || !HasImage(index))
            return false;

        _isTransitioning = false;
        ApplyCurrentImage(index);
        return true;
    }

    private void Initialize()
    {
        _isInitialized = false;

        if (!ValidateConfiguration())
            return;

        _currentImage = primaryImage;
        _nextImage = bufferImage;
        _restingPosition = primaryImage.rectTransform.anchoredPosition;

        int initialIndex = FindValidIndex(WrapIndex(startIndex), 1, true);
        if (initialIndex < 0)
        {
            Debug.LogWarning($"{nameof(UIImageSlideshow)} on '{name}' has no valid sprites.", this);
            return;
        }

        _isInitialized = true;
        ApplyCurrentImage(initialIndex);
    }

    private bool ValidateConfiguration()
    {
        if (imageCollection == null || primaryImage == null || bufferImage == null || viewport == null)
        {
            Debug.LogError(
                $"{nameof(UIImageSlideshow)} on '{name}' needs an image collection, two Images, and a viewport.",
                this);
            return false;
        }

        if (primaryImage == bufferImage)
        {
            Debug.LogError($"{nameof(UIImageSlideshow)} on '{name}' needs two different Image components.", this);
            return false;
        }

        if (primaryImage.rectTransform.parent != bufferImage.rectTransform.parent)
        {
            Debug.LogError($"Both slideshow Images on '{name}' must share the same parent.", this);
            return false;
        }

        return true;
    }

    private bool TryStartAdjacentTransition(int step)
    {
        if (!_isInitialized || _isTransitioning)
            return false;

        int targetIndex = FindValidIndex(_currentIndex, step, false);
        if (targetIndex < 0 || targetIndex == _currentIndex)
        {
            _displayTimer = 0f;
            return false;
        }

        BeginTransition(targetIndex, step);
        return true;
    }

    private void BeginTransition(int targetIndex, int navigationStep)
    {
        _targetIndex = targetIndex;
        _transitionTimer = 0f;
        _displayTimer = 0f;

        Vector2 viewportSize = viewport.rect.size;
        Vector2 forwardOffset = GetDirectionVector(direction);
        _travelOffset = Vector2.Scale(forwardOffset, viewportSize);
        if (navigationStep < 0)
            _travelOffset = -_travelOffset;

        _nextImage.sprite = imageCollection.GetImage(targetIndex);
        _nextImage.rectTransform.anchoredPosition = _restingPosition - _travelOffset;
        _nextImage.enabled = true;

        if (transitionDuration <= 0f)
        {
            CompleteTransition();
            return;
        }

        _isTransitioning = true;
    }

    private void UpdateTransition()
    {
        _transitionTimer += DeltaTime;
        float normalizedTime = Mathf.Clamp01(_transitionTimer / transitionDuration);
        float easedTime = easing != null ? easing.Evaluate(normalizedTime) : normalizedTime;

        _currentImage.rectTransform.anchoredPosition = _restingPosition + (_travelOffset * easedTime);
        _nextImage.rectTransform.anchoredPosition = _restingPosition - (_travelOffset * (1f - easedTime));

        if (normalizedTime >= 1f)
            CompleteTransition();
    }

    private void CompleteTransition()
    {
        _currentImage.rectTransform.anchoredPosition = _restingPosition;
        _currentImage.enabled = false;

        (_currentImage, _nextImage) = (_nextImage, _currentImage);
        _currentImage.rectTransform.anchoredPosition = _restingPosition;
        _currentIndex = _targetIndex;
        _targetIndex = -1;
        _transitionTimer = 0f;
        _isTransitioning = false;

        imageChanged?.Invoke(_currentIndex, _currentImage.sprite);
    }

    private void ApplyCurrentImage(int index)
    {
        _currentIndex = index;
        _targetIndex = -1;
        _displayTimer = 0f;
        _transitionTimer = 0f;

        _currentImage.sprite = imageCollection.GetImage(index);
        _currentImage.rectTransform.anchoredPosition = _restingPosition;
        _currentImage.enabled = true;

        _nextImage.rectTransform.anchoredPosition = _restingPosition;
        _nextImage.enabled = false;

        imageChanged?.Invoke(_currentIndex, _currentImage.sprite);
    }

    private int FindValidIndex(int origin, int step, bool includeOrigin)
    {
        int count = imageCollection.Count;
        if (count == 0)
            return -1;

        int index = includeOrigin ? origin : WrapIndex(origin + step);
        for (int checkedImages = 0; checkedImages < count; checkedImages++)
        {
            if (HasImage(index))
                return index;

            index = WrapIndex(index + step);
        }

        return -1;
    }

    private bool HasImage(int index)
    {
        return imageCollection != null && imageCollection.GetImage(index) != null;
    }

    private int WrapIndex(int index)
    {
        int count = imageCollection.Count;
        return count > 0 ? ((index % count) + count) % count : -1;
    }

    private static Vector2 GetDirectionVector(SlideshowDirection slideDirection)
    {
        return slideDirection switch
        {
            SlideshowDirection.Right => Vector2.right,
            SlideshowDirection.Up => Vector2.up,
            SlideshowDirection.Down => Vector2.down,
            _ => Vector2.left
        };
    }
}
