using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Shows one <see cref="ProjectMedia"/>. For videos (streamed from StreamingAssets into a RenderTexture):
/// shows the item's image as the poster, or the first frame from the <see cref="VideoPosterCache"/> when there
/// is none, and drives play/pause, a seek slider and a time label. Update only does work while a video is loaded.
/// </summary>
[DisallowMultipleComponent]
public class ProjectMediaViewer : MonoBehaviour
{
    [SerializeField] private Image image;
    [Tooltip("Optional. Envelope Parent makes the image fill (and crop to) its masked frame.")]
    [SerializeField] private AspectRatioFitter imageFitter;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Shows the playing video, or the first-frame poster before playback.")]
    [SerializeField] private RawImage videoSurface;
    [SerializeField] private AspectRatioFitter videoFitter;
    [SerializeField] private Vector2Int videoResolution = new(1280, 720);
    [Tooltip("Optional. Provides first-frame posters for videos without an image.")]
    [SerializeField] private VideoPosterCache posterCache;

    [Header("Video Controls")]
    [Tooltip("Large centred button, shown while the video isn't playing.")]
    [SerializeField] private Button playButton;
    [Tooltip("Bar with play/pause, seek and time. Only shown for videos.")]
    [SerializeField] private GameObject controlsRoot;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private GameObject playIcon;
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private Slider seekSlider;
    [Tooltip("Optional. Stops playback progress overwriting the slider while it's dragged.")]
    [SerializeField] private UIPointerHoldState seekHold;
    [SerializeField] private TMP_Text timeText;
    [Tooltip("Safety net: resume progress updates if the player never reports the seek as finished.")]
    [SerializeField, Min(0.1f)] private float seekTimeout = 1.5f;

    private RenderTexture _renderTexture;
    private ProjectMedia _media;
    private Action<VideoPoster> _applyPoster;
    private bool _playWhenPrepared;
    private bool _isSeeking;
    private float _seekStartTime;
    private int _shownSecond = -1;
    private int _shownLength = -1;

    public ProjectMedia Media => _media;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

    private bool CanPlayVideo => _media != null && _media.IsVideo && videoPlayer != null && videoSurface != null;

    private void Awake()
    {
        _applyPoster = ApplyRequestedPoster;

        if (playButton != null)
            playButton.onClick.AddListener(Play);
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(TogglePlayPause);
        if (seekSlider != null)
            seekSlider.onValueChanged.AddListener(Seek);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.prepareCompleted += HandlePrepared;
            videoPlayer.seekCompleted += HandleSeekCompleted;
            videoPlayer.loopPointReached += HandleLoopPointReached;
            videoPlayer.errorReceived += HandleVideoError;
        }
    }

    private void OnDisable()
    {
        StopVideo();
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(Play);
        if (playPauseButton != null)
            playPauseButton.onClick.RemoveListener(TogglePlayPause);
        if (seekSlider != null)
            seekSlider.onValueChanged.RemoveListener(Seek);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.seekCompleted -= HandleSeekCompleted;
            videoPlayer.loopPointReached -= HandleLoopPointReached;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    private void Update()
    {
        if (!CanPlayVideo || !videoPlayer.isPrepared)
            return;

        if (_isSeeking && Time.unscaledTime - _seekStartTime > seekTimeout)
            _isSeeking = false;

        // While dragging, or until the player catches up with a seek, its time is stale: leave the slider alone.
        bool isDragging = seekHold != null && seekHold.IsHeld;
        if (_isSeeking || isDragging)
            return;

        double length = videoPlayer.length;
        double time = videoPlayer.time;
        if (seekSlider != null && length > 0)
            seekSlider.SetValueWithoutNotify((float)(time / length));

        RefreshTimeText(time, length);
    }

    public void Show(ProjectMedia media)
    {
        StopVideo();
        _media = media;
        UIBinding.SetSprite(image, media?.Image, imageFitter);

        bool isVideo = CanPlayVideo;
        UIBinding.SetActive(controlsRoot, isVideo);
        RefreshPlayState();

        if (isVideo && media.Image == null)
            ShowPoster();
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
            Pause();
        else
            Play();
    }

    public void Play()
    {
        if (!CanPlayVideo)
            return;

        if (!videoPlayer.isPrepared)
        {
            Prepare();
            return;
        }

        videoSurface.texture = _renderTexture;
        SetVideoAspect(videoPlayer.width, videoPlayer.height);
        videoSurface.enabled = true;
        videoPlayer.Play();
        RefreshPlayState();
    }

    public void Pause()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
            return;

        videoPlayer.Pause();
        RefreshPlayState();
    }

    /// <param name="normalizedTime">0 = start, 1 = end.</param>
    public void Seek(float normalizedTime)
    {
        if (!CanPlayVideo || !videoPlayer.isPrepared || videoPlayer.length <= 0)
            return;

        double target = Mathf.Clamp01(normalizedTime) * videoPlayer.length;
        _isSeeking = true;
        _seekStartTime = Time.unscaledTime;
        videoPlayer.time = target;
        RefreshTimeText(target, videoPlayer.length);

        // A paused player only shows the new frame once it renders one; swap the poster for the video surface.
        videoSurface.texture = _renderTexture;
    }

    public void StopVideo()
    {
        _playWhenPrepared = false;
        _isSeeking = false;

        if (videoPlayer != null && (videoPlayer.isPlaying || videoPlayer.isPrepared))
            videoPlayer.Stop();
        if (videoSurface != null)
            videoSurface.enabled = false;

        if (seekSlider != null)
        {
            seekSlider.SetValueWithoutNotify(0f);
            seekSlider.interactable = false;
        }

        _shownSecond = -1;
        _shownLength = -1;
        RefreshTimeText(0, 0);
        RefreshPlayState();
    }

    private void Prepare()
    {
        EnsureRenderTexture();
        _playWhenPrepared = true;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = _media.VideoUrl;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _renderTexture;
        videoPlayer.Prepare();
        RefreshPlayState();
    }

    private void HandlePrepared(VideoPlayer player)
    {
        if (seekSlider != null)
            seekSlider.interactable = player.length > 0;

        if (_playWhenPrepared)
        {
            _playWhenPrepared = false;
            Play();
        }
    }

    private void HandleSeekCompleted(VideoPlayer player)
    {
        _isSeeking = false;
    }

    private void HandleLoopPointReached(VideoPlayer player)
    {
        if (!player.isLooping)
            RefreshPlayState();
    }

    private void HandleVideoError(VideoPlayer player, string message)
    {
        Debug.LogError($"{nameof(ProjectMediaViewer)}: video error '{message}' for URL {player.url}", this);
        StopVideo();
    }

    private void ShowPoster()
    {
        if (posterCache == null)
            return;

        if (posterCache.TryGet(_media.VideoUrl, out VideoPoster poster))
            ApplyPoster(poster);
        else
            posterCache.Request(_media.VideoUrl, _applyPoster);
    }

    private void ApplyRequestedPoster(VideoPoster poster)
    {
        // Ignore captures that finish after the user moved on or already started playback.
        if (this == null || !CanPlayVideo || _media.Image != null || videoPlayer.isPrepared || _playWhenPrepared)
            return;

        if (posterCache.TryGet(_media.VideoUrl, out VideoPoster current))
            ApplyPoster(current);
    }

    private void ApplyPoster(VideoPoster poster)
    {
        if (!poster.IsValid)
            return;

        videoSurface.texture = poster.Texture;
        if (videoFitter != null)
            videoFitter.aspectRatio = poster.AspectRatio;
        videoSurface.enabled = true;
    }

    private void SetVideoAspect(uint width, uint height)
    {
        if (videoFitter != null && height > 0)
            videoFitter.aspectRatio = width / (float)height;
    }

    /// <summary>Rebuilds the label only when the displayed second changes, so playback doesn't allocate every frame.</summary>
    private void RefreshTimeText(double time, double length)
    {
        if (timeText == null)
            return;

        int second = Mathf.FloorToInt((float)time);
        int lengthSecond = Mathf.FloorToInt((float)length);
        if (second == _shownSecond && lengthSecond == _shownLength)
            return;

        _shownSecond = second;
        _shownLength = lengthSecond;
        timeText.text = $"{second / 60}:{second % 60:00} / {lengthSecond / 60}:{lengthSecond % 60:00}";
    }

    private void RefreshPlayState()
    {
        bool isVideo = CanPlayVideo;
        bool isPlaying = isVideo && IsPlaying;
        bool isLoading = isVideo && _playWhenPrepared;

        UIBinding.SetActive(playButton, isVideo && !isPlaying && !isLoading);
        UIBinding.SetActive(playIcon, !isPlaying);
        UIBinding.SetActive(pauseIcon, isPlaying);
    }

    private void EnsureRenderTexture()
    {
        if (_renderTexture != null)
            return;

        _renderTexture = new RenderTexture(videoResolution.x, videoResolution.y, 0) { name = "Project Media Video" };
        _renderTexture.Create();
    }
}
