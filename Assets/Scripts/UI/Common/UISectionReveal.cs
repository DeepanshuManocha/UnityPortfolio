using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class UIRevealTarget
{
    [SerializeField] private RectTransform target;
    [Tooltip("Reveal each active child one by one instead of the target as a whole (e.g. a row of pills).")]
    [SerializeField] private bool staggerChildren;

    public RectTransform Target => target;
    public bool StaggerChildren => staggerChildren;
}

/// <summary>
/// Staggered fade + scale reveal for a UI section. Uses scale rather than position so it is safe
/// inside layout groups. Runs after data views so pooled children exist before they are collected.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class UISectionReveal : MonoBehaviour
{
    [Header("Targets (order = reveal order)")]
    [SerializeField] private List<UIRevealTarget> targets = new();

    [Header("Playback")]
    [Tooltip("Reveal automatically on enable, or when the activation camera is selected if one is assigned.")]
    [SerializeField] private bool playOnEnable = true;
    [Tooltip("Hide when the activation camera is left and reveal again on return.")]
    [SerializeField] private bool replayOnReactivate = true;
    [SerializeField] private bool useUnscaledTime;

    [Header("Camera Activation")]
    [Tooltip("When assigned, the reveal waits until Activation Camera is selected.")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CinemachineCamera activationCamera;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float startDelay = 0.15f;
    [SerializeField, Min(0f)] private float itemDuration = 0.45f;
    [SerializeField, Min(0f)] private float stagger = 0.06f;
    [SerializeField, Min(0f)] private float hideDuration = 0.2f;
    [SerializeField, Range(0.5f, 1f)] private float startScale = 0.92f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent revealCompleted;

    private readonly List<RevealElement> _elements = new();
    private readonly Dictionary<RectTransform, RevealElement> _elementCache = new();
    private Sequence _sequence;
    private bool _isActivationCameraActive;
    private bool _hasRevealed;

    public bool IsRevealed => _hasRevealed;

    private readonly struct RevealElement
    {
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        public readonly Vector3 RestingScale;

        public RevealElement(RectTransform rect, CanvasGroup group)
        {
            Rect = rect;
            Group = group;
            RestingScale = rect.localScale;
        }
    }

    private void OnEnable()
    {
        _hasRevealed = false;
        _isActivationCameraActive = false;
        SubscribeToCameraSwitcher();
        RefreshCameraState();
    }

    private void OnDisable()
    {
        UnsubscribeFromCameraSwitcher();
        KillSequence();
        ApplyVisibleState();
    }

    public void Play()
    {
        KillSequence();
        CollectElements();
        ApplyHiddenState();
        _hasRevealed = true;

        _sequence = CreateSequence();
        for (int i = 0; i < _elements.Count; i++)
        {
            RevealElement element = _elements[i];
            float startTime = startDelay + i * stagger;
            _sequence.Insert(startTime, element.Group.DOFade(1f, itemDuration).SetEase(easing));
            _sequence.Insert(startTime, element.Rect.DOScale(element.RestingScale, itemDuration).SetEase(easing));
        }

        _sequence.OnComplete(() =>
        {
            _sequence = null;
            revealCompleted?.Invoke();
        });
    }

    public void Hide(bool immediate = false)
    {
        KillSequence();
        CollectElements();
        _hasRevealed = false;

        if (immediate || hideDuration <= 0f)
        {
            ApplyHiddenState();
            return;
        }

        _sequence = CreateSequence();
        for (int i = 0; i < _elements.Count; i++)
            _sequence.Join(_elements[i].Group.DOFade(0f, hideDuration).SetEase(easing));

        _sequence.OnComplete(() =>
        {
            _sequence = null;
            ApplyHiddenState();
        });
    }

    public void ShowImmediately()
    {
        KillSequence();
        CollectElements();
        ApplyVisibleState();
        _hasRevealed = true;
    }

    private Sequence CreateSequence()
    {
        return DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }

    private void CollectElements()
    {
        _elements.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform target = targets[i]?.Target;
            if (target == null)
                continue;

            if (!targets[i].StaggerChildren)
            {
                AddElement(target);
                continue;
            }

            for (int childIndex = 0; childIndex < target.childCount; childIndex++)
            {
                if (target.GetChild(childIndex) is RectTransform child && child.gameObject.activeSelf)
                    AddElement(child);
            }
        }
    }

    private void AddElement(RectTransform rect)
    {
        if (!_elementCache.TryGetValue(rect, out RevealElement element))
        {
            if (!rect.TryGetComponent(out CanvasGroup group))
                group = rect.gameObject.AddComponent<CanvasGroup>();

            element = new RevealElement(rect, group);
            _elementCache.Add(rect, element);
        }

        _elements.Add(element);
    }

    private void ApplyHiddenState()
    {
        for (int i = 0; i < _elements.Count; i++)
        {
            RevealElement element = _elements[i];
            element.Group.alpha = 0f;
            element.Rect.localScale = element.RestingScale * startScale;
        }
    }

    private void ApplyVisibleState()
    {
        foreach (RevealElement element in _elementCache.Values)
        {
            if (element.Rect == null)
                continue;

            element.Group.alpha = 1f;
            element.Rect.localScale = element.RestingScale;
        }
    }

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
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
        bool isActive = !hasCameraGate || cameraSwitcher.CurrentCamera == activationCamera;
        if (!isActive)
            Hide(true);

        SetActivationCameraActive(isActive);
    }

    private void HandleCameraChanged(int _, CinemachineCamera activeCamera)
    {
        SetActivationCameraActive(activationCamera == null || activeCamera == activationCamera);
    }

    private void SetActivationCameraActive(bool isActive)
    {
        if (_isActivationCameraActive == isActive)
            return;

        _isActivationCameraActive = isActive;
        if (isActive)
        {
            if (playOnEnable && !_hasRevealed)
                Play();
            return;
        }

        if (replayOnReactivate && _hasRevealed)
            Hide();
    }
}
