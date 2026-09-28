using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Modal detail view for one <see cref="ProjectData"/>. Hides empty or missing parts instead of leaving gaps:
/// no media hides the hero (text takes the full width), a single media item hides the Media strip, and an
/// empty (or switched-off) tech stack hides its row. Layout groups reflow the rest.
/// </summary>
[DisallowMultipleComponent]
public class ProjectDetailView : MonoBehaviour
{
    [Header("Modal")]
    [Tooltip("Activated while open. Keep this component outside it so it can open the modal again.")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private CanvasGroup modalGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button closeButton;
    [Tooltip("Optional. Clicking outside the panel closes it.")]
    [SerializeField] private Button backdropButton;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Hero")]
    [Tooltip("Hidden when the project has no media, so the text takes the full width.")]
    [SerializeField] private GameObject heroRoot;
    [SerializeField] private ProjectMediaViewer heroViewer;

    [Header("About & Role")]
    [SerializeField] private GameObject aboutRoot;
    [SerializeField] private TMP_Text aboutText;
    [SerializeField] private GameObject roleRoot;
    [SerializeField] private TMP_Text roleText;

    [Header("Media Strip")]
    [Tooltip("Only shown when the project has more than one media item.")]
    [SerializeField] private GameObject mediaRoot;
    [SerializeField] private UIItemViewList<ProjectMediaThumbModel, ProjectMediaThumbView> mediaThumbs;
    [SerializeField, Min(1)] private int thumbsPerPage = 4;
    [SerializeField] private Button previousMediaButton;
    [SerializeField] private Button nextMediaButton;
    [SerializeField] private UIItemViewList<UIOptionModel, UIOptionButtonView> mediaDots;
    [SerializeField] private Color dotColor = new(0.23f, 0.8f, 0.98f, 1f);
    [Tooltip("Optional. Gives video thumbnails without an image their first frame.")]
    [SerializeField] private VideoPosterCache posterCache;

    [Header("Tech Stack")]
    [Tooltip("Untick to hide the Tech Stack for every project; the other sections grow into the space.")]
    [SerializeField] private bool showTechStack = true;
    [SerializeField] private GameObject techStackRoot;
    [SerializeField] private UIItemViewList<TechStackItem, TechStackItemView> techStack;

    [Header("Behaviour")]
    [Tooltip("Disabled while the modal is open, e.g. the camera scroll/swipe input.")]
    [SerializeField] private Behaviour[] disableWhileOpen;
    [SerializeField] private bool closeOnEscape = true;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float openDuration = 0.25f;
    [SerializeField, Min(0f)] private float closeDuration = 0.18f;
    [SerializeField, Range(0.5f, 1f)] private float startScale = 0.95f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent<ProjectData> opened;
    [SerializeField] private UnityEvent closed;

    private readonly List<ProjectMediaThumbModel> _thumbModels = new();
    private readonly List<UIOptionModel> _dotModels = new();
    private readonly List<TechStackItem> _techItems = new();

    private Action<int> _thumbClicked;
    private Action<int> _dotClicked;
    private TweenCallback _deactivateModal;

    private ProjectData _project;
    private Sequence _animation;
    private Vector3 _panelRestingScale = Vector3.one;
    private bool[] _disabledStates;
    private bool _isOpen;
    private bool _isRegisteredModal;
    private int _selectedMedia;
    private int _mediaPage;
    private int _mediaPageCount;

    public bool IsOpen => _isOpen;
    public ProjectData Project => _project;

    private void Awake()
    {
        _thumbClicked = SelectMedia;
        _dotClicked = ShowMediaPage;
        _deactivateModal = DeactivateModal;

        if (panel != null)
            _panelRestingScale = panel.localScale;

        AddListener(closeButton, Close);
        AddListener(backdropButton, Close);
        AddListener(previousMediaButton, PreviousMediaPage);
        AddListener(nextMediaButton, NextMediaPage);

        if (!_isOpen && modalRoot != null)
            modalRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        RemoveListener(closeButton, Close);
        RemoveListener(backdropButton, Close);
        RemoveListener(previousMediaButton, PreviousMediaPage);
        RemoveListener(nextMediaButton, NextMediaPage);
    }

    private void OnDisable()
    {
        _animation?.Kill();
        _animation = null;
        if (_isOpen)
            SetBlockedBehaviours(false);
        SetRegisteredModal(false);
    }

    private void Update()
    {
        if (_isOpen && closeOnEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open(ProjectData project)
    {
        if (project == null)
            return;

        _project = project;
        bool wasOpen = _isOpen;
        if (!wasOpen)
        {
            _isOpen = true;
            SetBlockedBehaviours(true);
            SetRegisteredModal(true);
            UIBinding.SetActive(modalRoot, true);
        }

        // Bind while active so new views initialise and layouts rebuild this frame.
        Bind(project);
        if (!wasOpen)
            PlayAnimation(true);

        opened?.Invoke(project);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        SetBlockedBehaviours(false);
        SetRegisteredModal(false);
        PlayAnimation(false);
        closed?.Invoke();
    }

    public void SelectMedia(int index)
    {
        IReadOnlyList<ProjectMedia> media = _project != null ? _project.Media : null;
        if (media == null || index < 0 || index >= media.Count)
            return;

        _selectedMedia = index;
        if (heroViewer != null)
            heroViewer.Show(media[index]);

        if (media.Count > 1)
            BindMediaStrip();
    }

    public void ShowMediaPage(int page)
    {
        if (page < 0 || page >= _mediaPageCount || page == _mediaPage)
            return;

        _mediaPage = page;
        BindMediaStrip();
    }

    public void NextMediaPage()
    {
        ShowMediaPage(_mediaPage + 1);
    }

    public void PreviousMediaPage()
    {
        ShowMediaPage(_mediaPage - 1);
    }

    private void Bind(ProjectData project)
    {
        UIBinding.SetText(titleText, project.Title);
        UIBinding.SetText(subtitleText, project.ShortDescription);
        BindBlock(aboutRoot, aboutText, project.About);
        BindBlock(roleRoot, roleText, project.Role);

        IReadOnlyList<ProjectMedia> media = project.Media;
        int mediaCount = media.Count;
        UIBinding.SetActive(heroRoot, mediaCount > 0);
        UIBinding.SetActive(mediaRoot, mediaCount > 1);

        _selectedMedia = 0;
        _mediaPage = 0;
        _mediaPageCount = Mathf.Max(1, Mathf.CeilToInt(mediaCount / (float)Mathf.Max(1, thumbsPerPage)));
        if (mediaCount > 0)
            SelectMedia(0);

        BindTechStack(project.TechStack);
    }

    private static void BindBlock(GameObject root, TMP_Text text, string value)
    {
        bool hasValue = !string.IsNullOrEmpty(value);
        UIBinding.SetActive(root, hasValue);
        if (hasValue)
            UIBinding.SetText(text, value);
    }

    private void BindMediaStrip()
    {
        IReadOnlyList<ProjectMedia> media = _project.Media;
        int perPage = Mathf.Max(1, thumbsPerPage);
        int start = _mediaPage * perPage;
        int end = Mathf.Min(start + perPage, media.Count);

        _thumbModels.Clear();
        for (int i = start; i < end; i++)
            _thumbModels.Add(new ProjectMediaThumbModel(i, media[i], i == _selectedMedia, _thumbClicked, posterCache));
        mediaThumbs.Bind(_thumbModels);

        bool hasPages = _mediaPageCount > 1;
        UIBinding.SetActive(previousMediaButton, hasPages);
        UIBinding.SetActive(nextMediaButton, hasPages);
        UIBinding.SetActive(mediaDots.Container, hasPages);
        if (!hasPages)
            return;

        if (previousMediaButton != null)
            previousMediaButton.interactable = _mediaPage > 0;
        if (nextMediaButton != null)
            nextMediaButton.interactable = _mediaPage < _mediaPageCount - 1;

        _dotModels.Clear();
        for (int i = 0; i < _mediaPageCount; i++)
            _dotModels.Add(new UIOptionModel(i, null, dotColor, i == _mediaPage, _dotClicked));
        mediaDots.Bind(_dotModels);
    }

    private void BindTechStack(IReadOnlyList<TechStackItem> items)
    {
        _techItems.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                _techItems.Add(items[i]);
        }

        bool isVisible = showTechStack && _techItems.Count > 0;
        UIBinding.SetActive(techStackRoot, isVisible);
        if (isVisible)
            techStack.Bind(_techItems);
    }

    private void PlayAnimation(bool isOpening)
    {
        _animation?.Kill();
        float duration = isOpening ? openDuration : closeDuration;
        if (modalGroup != null)
        {
            modalGroup.interactable = isOpening;
            modalGroup.blocksRaycasts = isOpening;
        }

        if (duration <= 0f || modalGroup == null)
        {
            if (modalGroup != null)
                modalGroup.alpha = isOpening ? 1f : 0f;
            if (panel != null)
                panel.localScale = _panelRestingScale;
            if (!isOpening)
                DeactivateModal();
            return;
        }

        if (isOpening)
        {
            modalGroup.alpha = 0f;
            if (panel != null)
                panel.localScale = _panelRestingScale * startScale;
        }

        _animation = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        _animation.Join(modalGroup.DOFade(isOpening ? 1f : 0f, duration).SetEase(isOpening ? Ease.OutQuad : Ease.InQuad));
        if (panel != null)
        {
            Vector3 targetScale = isOpening ? _panelRestingScale : _panelRestingScale * startScale;
            _animation.Join(panel.DOScale(targetScale, duration).SetEase(isOpening ? Ease.OutBack : Ease.InQuad));
        }

        if (!isOpening)
            _animation.OnComplete(_deactivateModal);
    }

    private void DeactivateModal()
    {
        _animation = null;
        if (!_isOpen && modalRoot != null)
            modalRoot.SetActive(false);
    }

    private void SetBlockedBehaviours(bool isBlocked)
    {
        if (disableWhileOpen == null)
            return;

        _disabledStates ??= new bool[disableWhileOpen.Length];
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            Behaviour behaviour = disableWhileOpen[i];
            if (behaviour == null)
                continue;

            if (isBlocked)
            {
                // Remember whether it was running so closing doesn't enable something that was off already.
                _disabledStates[i] = behaviour.enabled;
                behaviour.enabled = false;
            }
            else if (_disabledStates[i])
            {
                behaviour.enabled = true;
            }
        }
    }

    /// <summary>Reports open/close to <see cref="UIModalTracker"/> exactly once per state change.</summary>
    private void SetRegisteredModal(bool isOpen)
    {
        if (_isRegisteredModal == isOpen)
            return;

        _isRegisteredModal = isOpen;
        if (isOpen)
            UIModalTracker.NotifyOpened();
        else
            UIModalTracker.NotifyClosed();
    }

    private static void AddListener(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }
}
