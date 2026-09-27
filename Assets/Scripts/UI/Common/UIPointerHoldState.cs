using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reports whether the pointer is currently held down on this element, e.g. so a seek slider
/// isn't overwritten by playback progress while the user drags it.
/// </summary>
[DisallowMultipleComponent]
public class UIPointerHoldState : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsHeld = false;
    }

    private void OnDisable()
    {
        IsHeld = false;
    }
}
