using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class QuickLinkCustomAction
{
    [Tooltip("Matches the Value of a Custom quick link.")]
    [SerializeField] private string id;
    [SerializeField] private UnityEvent onInvoke = new();

    public string Id => id;
    public UnityEvent OnInvoke => onInvoke;

    public QuickLinkCustomAction()
    {
    }

    public QuickLinkCustomAction(string id)
    {
        this.id = id;
    }
}

/// <summary>
/// Side panel of quick links (LinkedIn, email, resume, custom actions). Web links open through
/// <see cref="WebLinkOpener"/>; Custom links invoke the matching scene action.
/// Hides through its CanvasGroup (alpha 0, not interactable, no raycasts), never by deactivating, so the layout
/// stays intact: while a modal (see <see cref="UIModalTracker"/>) is open and while one of the Hide On Cameras is
/// active. While the onboarding tour runs, only links marked Hide During Tour are concealed.
/// </summary>
[DisallowMultipleComponent]
public class QuickLinksView : MonoBehaviour
{
    [SerializeField] private QuickLinksData data;
    [SerializeField] private UIItemViewList<QuickLinkModel, QuickLinkButtonView> buttons;
    [SerializeField] private List<QuickLinkCustomAction> customActions = new();

    [Header("Visibility")]
    [SerializeField] private CanvasGroup group;
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [SerializeField] private bool hideWhileModalOpen = true;
    [Tooltip("Optional. While it runs, links marked Hide During Tour are concealed.")]
    [SerializeField] private OnboardingTourView onboardingTour;
    [Tooltip("Optional. Hidden while any of the Hide On Cameras is the active camera.")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private List<CinemachineCamera> hideOnCameras = new();

    private readonly List<QuickLinkModel> _models = new();
    private Action<QuickLink> _linkClicked;
    private Action<bool> _visibilityChanged;
    private Action<bool> _tourRunningChanged;
    private QuickLinksData _boundData;
    private Tween _fade;
    private bool _isVisible = true;

    public IReadOnlyList<QuickLinkCustomAction> CustomActions => customActions;

    private void Awake()
    {
        _linkClicked = HandleLinkClicked;
        _visibilityChanged = HandleVisibilityInputChanged;
        _tourRunningChanged = HandleTourRunningChanged;
    }

    private void OnEnable()
    {
        UIModalTracker.AnyOpenChanged += _visibilityChanged;
        if (onboardingTour != null)
            onboardingTour.RunningChanged += _tourRunningChanged;
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.AddListener(HandleCameraChanged);
        RefreshVisibility(true);

#if UNITY_EDITOR
        if (data != null)
            data.Changed += Refresh;
#endif
        if (_boundData != data)
            Refresh();
    }

    private void OnDisable()
    {
        UIModalTracker.AnyOpenChanged -= _visibilityChanged;
        if (onboardingTour != null)
            onboardingTour.RunningChanged -= _tourRunningChanged;
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.RemoveListener(HandleCameraChanged);
#if UNITY_EDITOR
        if (data != null)
            data.Changed -= Refresh;
#endif
        _fade?.Kill();
        _fade = null;
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        _boundData = data;
        if (data == null)
            return;

        _linkClicked ??= HandleLinkClicked;
        bool isTourRunning = onboardingTour != null && onboardingTour.IsRunning;

        _models.Clear();
        IReadOnlyList<QuickLink> links = data.Links;
        for (int i = 0; i < links.Count; i++)
        {
            QuickLink link = links[i];
            if (link != null && !link.IsHidden)
                _models.Add(new QuickLinkModel(link, _linkClicked, isTourRunning && link.HideDuringTour));
        }

        buttons.Bind(_models);
    }

    /// <summary>Runs a Custom action by id, e.g. from another script.</summary>
    public bool Invoke(string actionId)
    {
        for (int i = 0; i < customActions.Count; i++)
        {
            if (customActions[i] != null && customActions[i].Id == actionId)
            {
                customActions[i].OnInvoke?.Invoke();
                return true;
            }
        }

        Debug.LogWarning($"{nameof(QuickLinksView)} on '{name}' has no custom action '{actionId}'.", this);
        return false;
    }

    private void HandleLinkClicked(QuickLink link)
    {
        if (link.Type == QuickLinkType.Custom)
            Invoke(link.Value);
        else
            WebLinkOpener.Open(link.ResolveUrl(), link.Mode);
    }

    private void HandleVisibilityInputChanged(bool _)
    {
        RefreshVisibility(false);
    }

    private void HandleTourRunningChanged(bool _)
    {
        Refresh();
    }

    private void HandleCameraChanged(int _, CinemachineCamera __)
    {
        RefreshVisibility(false);
    }

    private void RefreshVisibility(bool immediate)
    {
        bool isHidden = (hideWhileModalOpen && UIModalTracker.IsAnyOpen) || IsOnHiddenCamera();
        SetVisible(!isHidden, immediate);
    }

    private bool IsOnHiddenCamera()
    {
        if (cameraSwitcher == null || hideOnCameras.Count == 0)
            return false;

        CinemachineCamera current = cameraSwitcher.CurrentCamera;
        return current != null && hideOnCameras.Contains(current);
    }

    private void SetVisible(bool isVisible, bool immediate)
    {
        if (group == null || (isVisible == _isVisible && !immediate))
            return;

        _isVisible = isVisible;
        _fade?.Kill();
        group.blocksRaycasts = isVisible;
        group.interactable = isVisible;

        float alpha = isVisible ? 1f : 0f;
        if (immediate || fadeDuration <= 0f)
        {
            group.alpha = alpha;
            return;
        }

        _fade = group.DOFade(alpha, fadeDuration).SetUpdate(true).SetLink(gameObject);
    }
}
