using UnityEngine;

/// <summary>Snapshot of a RectTransform's anchoring, for switching between authored and alternate layouts.</summary>
public readonly struct UIRectState
{
    public readonly Vector2 AnchorMin;
    public readonly Vector2 AnchorMax;
    public readonly Vector2 Pivot;
    public readonly Vector2 OffsetMin;
    public readonly Vector2 OffsetMax;

    public UIRectState(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        AnchorMin = anchorMin;
        AnchorMax = anchorMax;
        Pivot = pivot;
        OffsetMin = offsetMin;
        OffsetMax = offsetMax;
    }

    public static UIRectState Capture(RectTransform rect)
    {
        return new UIRectState(rect.anchorMin, rect.anchorMax, rect.pivot, rect.offsetMin, rect.offsetMax);
    }

    public void ApplyTo(RectTransform rect)
    {
        rect.anchorMin = AnchorMin;
        rect.anchorMax = AnchorMax;
        rect.pivot = Pivot;
        rect.offsetMin = OffsetMin;
        rect.offsetMax = OffsetMax;
    }
}
