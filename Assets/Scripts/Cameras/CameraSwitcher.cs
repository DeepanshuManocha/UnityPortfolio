using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;


public enum ActivationMode
{
    GameObjectToggle,
    Priority
}

[DisallowMultipleComponent]
public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras (order = navigation order)")]
    [SerializeField] private List<CinemachineCamera> cameras = new();

    [Header("Loop Behaviour")]
    [Tooltip("If true, going backwards from the first camera wraps to the last. Forward wrap (last -> first) is always enabled.")]
    [SerializeField] private bool loopBackwards = false;

    [Header("Activation")]
    [SerializeField] private ActivationMode activationMode = ActivationMode.GameObjectToggle;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    [Header("Startup")]
    [SerializeField, Min(0)] private int startIndex = 0;

    [Header("Transition Lock")]
    [Tooltip("CinemachineBrain whose blend state is used as the transition gate. Auto-found on Awake if left empty.")]
    [SerializeField] private CinemachineBrain brain;
    [Tooltip("Minimum time the transition lock is held after a switch, even if the brain reports no blend (covers the priority-change settle frame and instant cuts).")]
    [SerializeField, Min(0f)] private float minTransitionLock = 0.05f;

    [Header("Events")]
    public UnityEvent<int, CinemachineCamera> OnCameraChanged;

    private ICameraActivator _activator;
    private int _currentIndex = -1;
    private bool _transitionRequested;
    private float _transitionStartTime;

    public int CurrentIndex => _currentIndex;
    public int Count => cameras?.Count ?? 0;
    public CinemachineCamera CurrentCamera => IsValidIndex(_currentIndex) ? cameras[_currentIndex] : null;
    public bool LoopBackwards { get => loopBackwards; set => loopBackwards = value; }

    /// <summary>True while a camera switch is still resolving (blend in progress or grace window).</summary>
    public bool IsTransitioning
    {
        get
        {
            if (!_transitionRequested) return false;
            if (Time.unscaledTime - _transitionStartTime < minTransitionLock) return true;
            if (brain != null && brain.IsBlending) return true;
            _transitionRequested = false;
            return false;
        }
    }

    private void Awake()
    {
        _activator = CreateActivator();
        if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();
    }

    private void OnEnable()
    {
        if (cameras == null || cameras.Count == 0) return;

        for (int i = 0; i < cameras.Count; i++)
            _activator.Deactivate(cameras[i]);

        int initial = Mathf.Clamp(startIndex, 0, cameras.Count - 1);
        ApplyIndex(initial);
    }

    public bool Next()
    {
        if (Count == 0 || IsTransitioning) return false;
        int next = _currentIndex + 1;
        if (next >= Count) next = 0;
        ApplyIndex(next);
        return true;
    }

    public bool Previous()
    {
        if (Count == 0 || IsTransitioning) return false;
        int prev = _currentIndex - 1;
        if (prev < 0)
        {
            if (!loopBackwards) return false;
            prev = Count - 1;
        }
        ApplyIndex(prev);
        return true;
    }

    public bool GoTo(int index)
    {
        if (IsTransitioning || !IsValidIndex(index) || index == _currentIndex) return false;
        ApplyIndex(index);
        return true;
    }

    public void SetActivator(ICameraActivator activator)
    {
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
    }

    private void ApplyIndex(int newIndex)
    {
        if (IsValidIndex(_currentIndex))
            _activator.Deactivate(cameras[_currentIndex]);

        _currentIndex = newIndex;
        _activator.Activate(cameras[_currentIndex]);

        _transitionRequested = true;
        _transitionStartTime = Time.unscaledTime;

        OnCameraChanged?.Invoke(_currentIndex, cameras[_currentIndex]);
    }

    private bool IsValidIndex(int i) => i >= 0 && i < Count;

    private ICameraActivator CreateActivator()
    {
        return activationMode switch
        {
            ActivationMode.Priority => new PriorityActivator(activePriority, inactivePriority),
            _ => new GameObjectActivator()
        };
    }
}
