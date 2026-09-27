using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public readonly struct VideoPoster
{
    public readonly Texture Texture;
    public readonly float AspectRatio;

    public VideoPoster(Texture texture, float aspectRatio)
    {
        Texture = texture;
        AspectRatio = aspectRatio;
    }

    public bool IsValid => Texture != null;
}

/// <summary>
/// Captures the first frame of videos (one at a time, with a single hidden VideoPlayer) into small
/// RenderTextures and caches them by URL, so thumbnails and hero posters can show a real frame without
/// keeping a player per video. Update only runs while a capture is in progress.
/// </summary>
[DisallowMultipleComponent]
public class VideoPosterCache : MonoBehaviour
{
    [SerializeField] private VideoPlayer capturePlayer;
    [SerializeField, Min(64)] private int posterWidth = 480;
    [Tooltip("Seconds before a capture that hasn't produced a frame is given up.")]
    [SerializeField, Min(1f)] private float captureTimeout = 10f;

    private readonly Dictionary<string, VideoPoster> _posters = new();
    private readonly Dictionary<string, Action<VideoPoster>> _pending = new();
    private readonly Queue<string> _queue = new();

    private string _currentUrl;
    private float _captureStartTime;

    private void Awake()
    {
        if (capturePlayer == null)
            capturePlayer = gameObject.AddComponent<VideoPlayer>();

        capturePlayer.playOnAwake = false;
        capturePlayer.isLooping = false;
        capturePlayer.renderMode = VideoRenderMode.APIOnly;
        capturePlayer.audioOutputMode = VideoAudioOutputMode.None;
        capturePlayer.source = VideoSource.Url;
        capturePlayer.prepareCompleted += HandlePrepared;
        capturePlayer.errorReceived += HandleError;
        enabled = false;
    }

    private void OnDestroy()
    {
        if (capturePlayer != null)
        {
            capturePlayer.prepareCompleted -= HandlePrepared;
            capturePlayer.errorReceived -= HandleError;
        }

        foreach (VideoPoster poster in _posters.Values)
        {
            if (poster.Texture is RenderTexture renderTexture)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }

    private void Update()
    {
        if (_currentUrl == null)
        {
            enabled = false;
            return;
        }

        // The first decoded frame is available once the player has advanced past frame 0.
        if (capturePlayer.isPlaying && capturePlayer.frame > 0 && capturePlayer.texture != null)
        {
            Complete(CopyFrame(capturePlayer.texture));
            return;
        }

        if (Time.unscaledTime - _captureStartTime > captureTimeout)
            Complete(default);
    }

    public bool TryGet(string url, out VideoPoster poster)
    {
        return _posters.TryGetValue(url, out poster);
    }

    /// <summary>Calls <paramref name="onReady"/> with the poster, immediately if cached. Invalid poster on failure.</summary>
    public void Request(string url, Action<VideoPoster> onReady)
    {
        if (string.IsNullOrEmpty(url))
            return;

        if (_posters.TryGetValue(url, out VideoPoster poster))
        {
            onReady?.Invoke(poster);
            return;
        }

        if (_pending.TryGetValue(url, out Action<VideoPoster> callbacks))
        {
            _pending[url] = callbacks + onReady;
            return;
        }

        _pending[url] = onReady;
        _queue.Enqueue(url);
        if (_currentUrl == null)
            StartNext();
    }

    private void StartNext()
    {
        _currentUrl = null;
        while (_queue.Count > 0)
        {
            string url = _queue.Dequeue();
            if (_posters.ContainsKey(url))
                continue;

            _currentUrl = url;
            _captureStartTime = Time.unscaledTime;
            capturePlayer.url = url;
            capturePlayer.Prepare();
            enabled = true;
            return;
        }
    }

    private void HandlePrepared(VideoPlayer player)
    {
        if (_currentUrl != null)
            player.Play();
    }

    private void HandleError(VideoPlayer player, string message)
    {
        Debug.LogWarning($"{nameof(VideoPosterCache)}: couldn't capture a poster for {player.url} ({message}).", this);
        if (_currentUrl != null)
            Complete(default);
    }

    private VideoPoster CopyFrame(Texture source)
    {
        float aspect = source.height > 0 ? source.width / (float)source.height : 16f / 9f;
        int height = Mathf.Max(1, Mathf.RoundToInt(posterWidth / aspect));
        var poster = new RenderTexture(posterWidth, height, 0) { name = "Video Poster" };
        poster.Create();
        Graphics.Blit(source, poster);
        return new VideoPoster(poster, aspect);
    }

    private void Complete(VideoPoster poster)
    {
        string url = _currentUrl;
        capturePlayer.Stop();

        // Cache failures too, so a broken video isn't retried on every bind.
        _posters[url] = poster;
        if (_pending.Remove(url, out Action<VideoPoster> callbacks))
            callbacks?.Invoke(poster);

        StartNext();
    }
}
