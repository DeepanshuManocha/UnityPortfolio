using DG.Tweening;
using Unity.Cinemachine;
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

    [Header("Camera Activation")]
    [Tooltip("When assigned, automatic playback only runs while Activation Camera is selected.")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CinemachineCamera activationCamera;

    [Header("Transition")]
    [SerializeField] private SlideshowDirection direction = SlideshowDirection.Left;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0.8f, 1f)] private float incomingScale = 0.96f;
    [SerializeField, Range(0f, 1f)] private float outgoingAlpha = 0.35f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int, Sprite> imageChanged;

    private Image _currentImage;
    private Image _nextImage;
    private Vector2 _restingPosition;
    private Vector3 _primaryRestingScale;
    private Vector3 _bufferRestingScale;
    private float _primaryRestingAlpha;
    private float _bufferRestingAlpha;
    private int _currentIndex = -1;
    private int _targetIndex = -1;
    private bool _isInitialized;
    private bool _isPlaying;
    private bool _isTransitioning;
    private bool _isActivationCameraActive;
    private Tween _autoAdvanceTween;
    private Sequence _transitionSequence;

    public int CurrentIndex => _currentIndex;
    public Sprite CurrentSprite => _currentImage != null ? _currentImage.sprite : null;
    public bool IsPlaying => _isPlaying;
    public bool IsTransitioning => _isTransitioning;

    private void Reset()
    {
        viewport = transform as RectTransform;
    }

    private void OnEnable()
    {
        _isPlaying = playOnEnable;
        Initialize();
        SubscribeToCameraSwitcher();
        RefreshCameraState();
    }

    private void OnDisable()
    {
        UnsubscribeFromCameraSwitcher();
        CancelAutoAdvance();
        CancelTransition();

        if (!_isInitialized)
            return;

        ResetImageVisuals();
    }

    public void Play()
    {
        _isPlaying = true;
        ScheduleNextImage();
    }

    public void Pause()
    {
        _isPlaying = false;
        CancelAutoAdvance();
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
        if (!_isInitialized || !_isActivationCameraActive || _isTransitioning || !HasImage(index) || index == _currentIndex)
            return false;

        BeginTransition(index, 1);
        return true;
    }

    public bool ShowImmediately(int index)
    {
        if (!_isInitialized || !HasImage(index))
            return false;

        CancelTransition();
        ApplyCurrentImage(index);
        ScheduleNextImage();
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
        _primaryRestingScale = primaryImage.rectTransform.localScale;
        _bufferRestingScale = bufferImage.rectTransform.localScale;
        _primaryRestingAlpha = primaryImage.color.a;
        _bufferRestingAlpha = bufferImage.color.a;

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
        if (!_isInitialized || !_isActivationCameraActive || _isTransitioning)
            return false;

        int targetIndex = FindValidIndex(_currentIndex, step, false);
        if (targetIndex < 0 || targetIndex == _currentIndex)
        {
            ScheduleNextImage();
            return false;
        }

        BeginTransition(targetIndex, step);
        return true;
    }

    private void BeginTransition(int targetIndex, int navigationStep)
    {
        CancelAutoAdvance();
        _targetIndex = targetIndex;

        Vector2 viewportSize = viewport.rect.size;
        Vector2 forwardOffset = GetDirectionVector(direction);
        Vector2 travelOffset = Vector2.Scale(forwardOffset, viewportSize);
        if (navigationStep < 0)
            travelOffset = -travelOffset;

        _nextImage.sprite = imageCollection.GetImage(targetIndex);
        _nextImage.rectTransform.anchoredPosition = _restingPosition - travelOffset;
        _nextImage.rectTransform.localScale = GetRestingScale(_nextImage) * incomingScale;
        SetAlpha(_nextImage, 0f);
        _nextImage.enabled = true;

        if (transitionDuration <= 0f)
        {
            CompleteTransition();
            return;
        }

        _isTransitioning = true;
        _transitionSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        _transitionSequence.Join(_currentImage.rectTransform
            .DOAnchorPos(_restingPosition + travelOffset, transitionDuration)
            .SetEase(easing));
        _transitionSequence.Join(_nextImage.rectTransform
            .DOAnchorPos(_restingPosition, transitionDuration)
            .SetEase(easing));
        _transitionSequence.Join(_currentImage
            .DOFade(GetRestingAlpha(_currentImage) * outgoingAlpha, transitionDuration)
            .SetEase(easing));
        _transitionSequence.Join(_nextImage
            .DOFade(GetRestingAlpha(_nextImage), transitionDuration)
            .SetEase(easing));
        _transitionSequence.Join(_currentImage.rectTransform
            .DOScale(GetRestingScale(_currentImage) * incomingScale, transitionDuration)
            .SetEase(easing));
        _transitionSequence.Join(_nextImage.rectTransform
            .DOScale(GetRestingScale(_nextImage), transitionDuration)
            .SetEase(easing));
        _transitionSequence.OnComplete(CompleteTransition);
    }

    private void CompleteTransition()
    {
        _transitionSequence = null;
        _currentImage.rectTransform.anchoredPosition = _restingPosition;
        _currentImage.rectTransform.localScale = GetRestingScale(_currentImage);
        SetAlpha(_currentImage, GetRestingAlpha(_currentImage));
        _currentImage.enabled = false;

        (_currentImage, _nextImage) = (_nextImage, _currentImage);
        _currentImage.rectTransform.anchoredPosition = _restingPosition;
        _currentImage.rectTransform.localScale = GetRestingScale(_currentImage);
        SetAlpha(_currentImage, GetRestingAlpha(_currentImage));
        _currentIndex = _targetIndex;
        _targetIndex = -1;
        _isTransitioning = false;

        imageChanged?.Invoke(_currentIndex, _currentImage.sprite);
        ScheduleNextImage();
    }

    private void ApplyCurrentImage(int index)
    {
        _currentIndex = index;
        _targetIndex = -1;

        _currentImage.sprite = imageCollection.GetImage(index);
        _currentImage.enabled = true;
        _nextImage.enabled = false;
        ResetImageVisuals();

        imageChanged?.Invoke(_currentIndex, _currentImage.sprite);
    }

    private void SubscribeToCameraSwitcher()
    {
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.AddListener(HandleCameraChanged);
    }

    private void UnsubscribeFromCameraSwitcher()
    {
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.RemoveListener(HandleCameraChanged);
    }

    private void RefreshCameraState()
    {
        bool hasCameraGate = cameraSwitcher != null && activationCamera != null;
        SetActivationCameraActive(!hasCameraGate || cameraSwitcher.CurrentCamera == activationCamera);
    }

    private void HandleCameraChanged(int _, CinemachineCamera activeCamera)
    {
        SetActivationCameraActive(activationCamera == null || activeCamera == activationCamera);
    }

    private void SetActivationCameraActive(bool isActive)
    {
        if (_isActivationCameraActive == isActive)
        {
            if (isActive)
                ScheduleNextImage();
            return;
        }

        _isActivationCameraActive = isActive;
        if (isActive)
        {
            ScheduleNextImage();
            return;
        }

        CancelAutoAdvance();
        CancelTransition();
    }

    private void ScheduleNextImage()
    {
        CancelAutoAdvance();

        if (!_isInitialized || !_isPlaying || !_isActivationCameraActive || _isTransitioning)
            return;

        _autoAdvanceTween = DOVirtual.DelayedCall(imageDuration, () =>
            {
                _autoAdvanceTween = null;
                Next();
            }, useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void CancelAutoAdvance()
    {
        _autoAdvanceTween?.Kill();
        _autoAdvanceTween = null;
    }

    private void CancelTransition()
    {
        _transitionSequence?.Kill();
        _transitionSequence = null;
        _targetIndex = -1;
        _isTransitioning = false;

        if (_isInitialized)
            ResetImageVisuals();
    }

    private void ResetImageVisuals()
    {
        ResetImageVisual(primaryImage, _primaryRestingScale, _primaryRestingAlpha);
        ResetImageVisual(bufferImage, _bufferRestingScale, _bufferRestingAlpha);

        if (_currentImage != null)
            _currentImage.enabled = true;
        if (_nextImage != null)
            _nextImage.enabled = false;
    }

    private void ResetImageVisual(Image image, Vector3 scale, float alpha)
    {
        image.rectTransform.anchoredPosition = _restingPosition;
        image.rectTransform.localScale = scale;
        SetAlpha(image, alpha);
    }

    private Vector3 GetRestingScale(Image image)
    {
        return image == primaryImage ? _primaryRestingScale : _bufferRestingScale;
    }

    private float GetRestingAlpha(Image image)
    {
        return image == primaryImage ? _primaryRestingAlpha : _bufferRestingAlpha;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
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
