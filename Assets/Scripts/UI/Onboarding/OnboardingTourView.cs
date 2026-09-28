using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// First-visit tour panel. Each step belongs to a camera: when the camera changes the panel hides, and once the
/// camera transition settles it shows that camera's step. The X button hides the panel until the next transition.
/// Moving on from the last step ends the tour and remembers it in PlayerPrefs (IndexedDB on WebGL).
/// The panel also steps aside while any modal (see <see cref="UIModalTracker"/>) is open.
/// </summary>
[DisallowMultipleComponent]
public class OnboardingTourView : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private OnboardingTourData data;
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("Panel")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text eyebrowText;
    [SerializeField] private UIItemViewList<UIOptionModel, UIOptionButtonView> stepDashes;
    [SerializeField] private Color dashColor = new(0.55f, 0.4f, 1f, 1f);
    [SerializeField] private TMP_Text currentStepText;
    [SerializeField] private TMP_Text totalStepsText;

    [Header("Step")]
    [SerializeField] private TMP_Text titleLeadText;
    [SerializeField] private TMP_Text titleHighlightText;
    [SerializeField] private TMPTextGradient titleHighlightGradient;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject instructionRoot;
    [SerializeField] private Image instructionIcon;
    [SerializeField] private TMP_Text instructionText;

    [Header("Persistence")]
    [SerializeField] private bool showOnlyOnce = true;
    [SerializeField] private string completedKey = "Portfolio.OnboardingTour.Completed";
    [Tooltip("Editor only: ignore the saved flag so the tour shows on every Play.")]
    [SerializeField] private bool alwaysShowInEditor;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float showDelay = 0.6f;
    [SerializeField, Min(0f)] private float panelFadeDuration = 0.35f;
    [SerializeField] private float panelSlideDistance = 40f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent completed;

    private readonly List<UIOptionModel> _dashModels = new();
    private Action<int> _dashClicked;
    private Action<bool> _modalChanged;
    private TweenCallback _clearPanelTween;
    private TweenCallback _deactivate;
    private Sequence _panelTween;
    private Vector2 _panelRestingPosition;
    private string _highlightHex;
    private int _stepIndex = -1;
    private int _pendingCameraIndex = -1;
    private bool _isRunning;
    private bool _isDismissed;
    private bool _isWaitingForTransition;
    private bool _hasReachedLastStep;
    private bool _isPanelVisible = true;

    public bool IsRunning => _isRunning;

    /// <summary>Raised when the tour starts (true) and when it ends (false).</summary>
    public event Action<bool> RunningChanged;
    public int StepIndex => _stepIndex;

    private void Awake()
    {
        _dashClicked = GoToStep;
        _modalChanged = HandleModalChanged;
        _clearPanelTween = ClearPanelTween;
        _deactivate = Deactivate;
        if (panel != null)
            _panelRestingPosition = panel.anchoredPosition;
        if (closeButton != null)
            closeButton.onClick.AddListener(Dismiss);
    }

    private void Start()
    {
        if (data == null || data.Steps.Count == 0 || HasCompleted())
        {
            Deactivate();
            return;
        }

        Begin(showDelay);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Dismiss);
        StopListening();
        _panelTween?.Kill();
    }

    /// <summary>Only runs while waiting for a camera transition to settle.</summary>
    private void Update()
    {
        if (!_isWaitingForTransition)
        {
            enabled = false;
            return;
        }

        if (cameraSwitcher != null && cameraSwitcher.IsTransitioning)
            return;

        _isWaitingForTransition = false;
        enabled = false;
        ShowStepForCamera(_pendingCameraIndex, 0f);
    }

    /// <summary>X button: hides the panel until the next camera transition finishes; on the last step it ends the tour.</summary>
    public void Dismiss()
    {
        if (!_isRunning)
            return;

        // Closing the last step finishes the tour (which also brings back tour-hidden quick links).
        if (_hasReachedLastStep && _stepIndex == data.Steps.Count - 1)
        {
            Complete();
            return;
        }

        _isDismissed = true;
        RefreshVisibility(0f);
    }

    /// <summary>Moves the camera to the step's camera; the step shows once the transition settles.</summary>
    public void GoToStep(int stepIndex)
    {
        if (!_isRunning || stepIndex < 0 || stepIndex >= data.Steps.Count)
            return;

        if (cameraSwitcher != null)
            cameraSwitcher.GoTo(data.Steps[stepIndex].CameraIndex);
        else
            ShowStep(stepIndex, 0f);
    }

    /// <summary>Clears the saved progress and runs the tour again from the step of the current camera.</summary>
    public void RestartFromCurrentCamera()
    {
        ResetProgress();
        if (data == null || data.Steps.Count == 0)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _panelTween?.Kill();
        _panelTween = null;
        StopListening();
        Begin(0f);
    }

    [ContextMenu("Reset Tour Progress")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(completedKey);
        PlayerPrefs.Save();
    }

    private void Begin(float delay)
    {
        bool wasRunning = _isRunning;
        _isRunning = true;
        _isDismissed = false;
        _isWaitingForTransition = false;
        _hasReachedLastStep = false;
        _stepIndex = -1;
        _highlightHex = ColorUtility.ToHtmlStringRGB(data.InstructionHighlightColor);
        UIBinding.SetText(eyebrowText, data.Eyebrow);
        UIBinding.SetText(totalStepsText, "/ " + data.Steps.Count.ToString("00"));

        StartListening();
        SetPanelHidden();
        enabled = false;

        int cameraIndex = cameraSwitcher != null ? cameraSwitcher.CurrentIndex : data.Steps[0].CameraIndex;
        ShowStepForCamera(cameraIndex, delay);
        if (!wasRunning)
            RunningChanged?.Invoke(true);
    }

    private void Complete()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _isWaitingForTransition = false;
        if (showOnlyOnce)
        {
            PlayerPrefs.SetInt(completedKey, 1);
            PlayerPrefs.Save(); // flushes to IndexedDB on WebGL
        }

        StopListening();
        PlayPanel(false, 0f);
        RunningChanged?.Invoke(false);
        completed?.Invoke();
    }

    private bool HasCompleted()
    {
#if UNITY_EDITOR
        if (alwaysShowInEditor)
            return false;
#endif
        return showOnlyOnce && PlayerPrefs.GetInt(completedKey, 0) == 1;
    }

    private void HandleCameraChanged(int cameraIndex, CinemachineCamera _)
    {
        // The step after the last one is "leaving" the tour.
        if (_hasReachedLastStep)
        {
            Complete();
            return;
        }

        _pendingCameraIndex = cameraIndex;
        _isDismissed = false;
        _isWaitingForTransition = true;
        enabled = true;
        RefreshVisibility(0f);
    }

    private void HandleModalChanged(bool isAnyOpen)
    {
        RefreshVisibility(0f);
    }

    private void ShowStepForCamera(int cameraIndex, float delay)
    {
        int step = data.FindStepForCamera(cameraIndex);
        if (step < 0)
        {
            _stepIndex = -1;
            RefreshVisibility(0f);
            return;
        }

        ShowStep(step, delay);
    }

    private void ShowStep(int stepIndex, float delay)
    {
        _stepIndex = stepIndex;
        if (stepIndex == data.Steps.Count - 1)
            _hasReachedLastStep = true;

        BindStep();
        RefreshVisibility(delay);
    }

    private void BindStep()
    {
        IReadOnlyList<OnboardingStep> steps = data.Steps;
        OnboardingStep step = steps[_stepIndex];

        UIBinding.SetText(titleLeadText, step.TitleLead);
        UIBinding.SetText(titleHighlightText, step.TitleHighlight);
        if (titleHighlightGradient != null)
            titleHighlightGradient.SetGradient(data.TitleHighlightGradient);
        UIBinding.SetText(descriptionText, step.Description);

        bool hasInstruction = step.HasInstruction;
        UIBinding.SetActive(instructionRoot, hasInstruction);
        if (hasInstruction)
        {
            UIBinding.SetIcon(instructionIcon, step.InstructionIcon);
            UIBinding.SetText(instructionText, FormatInstruction(step));
        }

        UIBinding.SetText(currentStepText, (_stepIndex + 1).ToString("00"));

        _dashModels.Clear();
        for (int i = 0; i < steps.Count; i++)
            _dashModels.Add(new UIOptionModel(i, null, dashColor, i == _stepIndex, _dashClicked));
        stepDashes.Bind(_dashModels);
    }

    private string FormatInstruction(OnboardingStep step)
    {
        if (string.IsNullOrEmpty(step.InstructionHighlight))
            return step.Instruction;

        return $"<color=#{_highlightHex}>{step.InstructionHighlight}</color> {step.Instruction}";
    }

    private void RefreshVisibility(float delay)
    {
        bool isVisible = _isRunning && !_isDismissed && !_isWaitingForTransition && _stepIndex >= 0 &&
                         !UIModalTracker.IsAnyOpen;
        PlayPanel(isVisible, delay);
    }

    private void SetPanelHidden()
    {
        _isPanelVisible = false;
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
        }

        if (panel != null)
            panel.anchoredPosition = _panelRestingPosition + Vector2.down * panelSlideDistance;
    }

    private void PlayPanel(bool show, float delay)
    {
        if (show == _isPanelVisible && _panelTween == null)
        {
            // Already hidden when the tour ends (e.g. dismissed on the last step): switch off straight away.
            if (!show && !_isRunning)
                Deactivate();
            return;
        }

        _isPanelVisible = show;
        _panelTween?.Kill();
        if (panelGroup != null)
            panelGroup.blocksRaycasts = show;

        Vector2 target = show ? _panelRestingPosition : _panelRestingPosition + Vector2.down * panelSlideDistance;
        _panelTween = DOTween.Sequence().SetUpdate(useUnscaledTime).SetLink(gameObject).SetDelay(delay);
        if (panelGroup != null)
            _panelTween.Join(panelGroup.DOFade(show ? 1f : 0f, panelFadeDuration).SetEase(show ? Ease.OutQuad : Ease.InQuad));
        if (panel != null)
            _panelTween.Join(panel.DOAnchorPos(target, panelFadeDuration).SetEase(show ? Ease.OutCubic : Ease.InCubic));

        // A finished tour switches the whole object off once faded, so nothing is left rendering or listening.
        _panelTween.OnComplete(_isRunning ? _clearPanelTween : _deactivate);
    }

    private void ClearPanelTween()
    {
        _panelTween = null;
    }

    private void Deactivate()
    {
        _panelTween = null;
        gameObject.SetActive(false);
    }

    private void StartListening()
    {
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.AddListener(HandleCameraChanged);
        UIModalTracker.AnyOpenChanged += _modalChanged;
#if UNITY_EDITOR
        data.Changed += RefreshFromData;
#endif
    }

    private void StopListening()
    {
        if (cameraSwitcher != null)
            cameraSwitcher.OnCameraChanged.RemoveListener(HandleCameraChanged);
        UIModalTracker.AnyOpenChanged -= _modalChanged;
#if UNITY_EDITOR
        if (data != null)
            data.Changed -= RefreshFromData;
#endif
    }

    private void RefreshFromData()
    {
        if (!_isRunning || data.Steps.Count == 0)
            return;

        _highlightHex = ColorUtility.ToHtmlStringRGB(data.InstructionHighlightColor);
        UIBinding.SetText(eyebrowText, data.Eyebrow);
        UIBinding.SetText(totalStepsText, "/ " + data.Steps.Count.ToString("00"));
        if (_stepIndex >= 0)
        {
            _stepIndex = Mathf.Min(_stepIndex, data.Steps.Count - 1);
            BindStep();
        }
    }
}
