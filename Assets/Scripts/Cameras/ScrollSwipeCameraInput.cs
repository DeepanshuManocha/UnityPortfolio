using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CameraSwitcher))]
public class ScrollSwipeCameraInput : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private CameraSwitcher switcher;

    [Header("Mouse Scroll")]
    [Tooltip("Minimum absolute scroll Y delta to register a step.")]
    [SerializeField] private float scrollThreshold = 0.1f;

    [Header("Touch Swipe")]
    [Tooltip("Minimum vertical screen-pixel distance a finger must travel to register a swipe.")]
    [SerializeField] private float swipeThreshold = 75f;

    private bool _swipeTracking;
    private Vector2 _swipeStart;

    private void Reset()
    {
        switcher = GetComponent<CameraSwitcher>();
    }

    private void Awake()
    {
        if (switcher == null) switcher = GetComponent<CameraSwitcher>();
    }

    private void Update()
    {
        // Drop all input while a transition is in flight. While locked we also
        // reset swipe tracking so the finger has to lift and re-press to switch again.
        if (switcher.IsTransitioning)
        {
            _swipeTracking = false;
            return;
        }

        if (HandleScroll()) return;
        HandleTouchSwipe();
    }

    private bool HandleScroll()
    {
        var mouse = Mouse.current;
        if (mouse == null) return false;

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < scrollThreshold) return false;

        // Scroll wheel DOWN (negative) -> Next, wheel UP (positive) -> Previous.
        if (scrollY < 0f) switcher.Next();
        else switcher.Previous();
        return true;
    }

    private void HandleTouchSwipe()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        var primary = touchscreen.primaryTouch;
        var phase = primary.phase.ReadValue();

        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                _swipeTracking = true;
                _swipeStart = primary.position.ReadValue();
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:
            case UnityEngine.InputSystem.TouchPhase.Stationary:
                if (!_swipeTracking) break;
                Vector2 cur = primary.position.ReadValue();
                float dy = cur.y - _swipeStart.y;
                if (Mathf.Abs(dy) >= swipeThreshold)
                {
                    // Swipe UP (positive dy) -> Next, swipe DOWN (negative dy) -> Previous.
                    if (dy > 0f) switcher.Next();
                    else switcher.Previous();
                    _swipeTracking = false;
                }
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:
            case UnityEngine.InputSystem.TouchPhase.Canceled:
            case UnityEngine.InputSystem.TouchPhase.None:
                _swipeTracking = false;
                break;
        }
    }
}