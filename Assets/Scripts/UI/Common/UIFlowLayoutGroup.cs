using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Places children left to right at their preferred size and wraps to a new row when the width runs out,
/// like inline tags. Child Alignment positions each row horizontally and the block vertically.
/// </summary>
[AddComponentMenu("Layout/Flow Layout Group")]
public class UIFlowLayoutGroup : LayoutGroup
{
    [SerializeField] private Vector2 spacing = new(22f, 22f);
    [Tooltip("Caps the preferred width reported to a parent layout, which is what makes rows wrap there. 0 = no cap.")]
    [SerializeField, Min(0f)] private float maxWidth;

    public Vector2 Spacing
    {
        get => spacing;
        set => SetProperty(ref spacing, value);
    }

    public float MaxWidth
    {
        get => maxWidth;
        set => SetProperty(ref maxWidth, value);
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float minWidth = 0f;
        float singleRowWidth = 0f;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            minWidth = Mathf.Max(minWidth, LayoutUtility.GetMinWidth(child));
            singleRowWidth += LayoutUtility.GetPreferredWidth(child) + (i > 0 ? spacing.x : 0f);
        }

        float preferredWidth = padding.horizontal + singleRowWidth;
        if (maxWidth > 0f)
            preferredWidth = Mathf.Min(preferredWidth, maxWidth);

        SetLayoutInputForAxis(padding.horizontal + minWidth, preferredWidth, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float height = ArrangeRows(-1, 0f) + padding.vertical;
        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeRows(0, 0f);
    }

    public override void SetLayoutVertical()
    {
        float contentHeight = ArrangeRows(-1, 0f);
        ArrangeRows(1, GetStartOffset(1, contentHeight));
    }

    /// <summary>
    /// Walks the children in rows and returns the content height (without padding).
    /// Axis -1 only measures; 0 or 1 also positions children along that axis.
    /// </summary>
    private float ArrangeRows(int axis, float startY)
    {
        float innerWidth = rectTransform.rect.width - padding.horizontal;
        float y = 0f;
        float rowWidth = 0f;
        float rowHeight = 0f;
        int rowStart = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float childWidth = Mathf.Min(LayoutUtility.GetPreferredWidth(child), innerWidth);
            if (i > rowStart && rowWidth + spacing.x + childWidth > innerWidth)
            {
                PlaceRow(axis, rowStart, i, rowWidth, rowHeight, innerWidth, startY + y);
                y += rowHeight + spacing.y;
                rowStart = i;
                rowWidth = 0f;
                rowHeight = 0f;
            }

            rowWidth += childWidth + (i > rowStart ? spacing.x : 0f);
            rowHeight = Mathf.Max(rowHeight, LayoutUtility.GetPreferredHeight(child));
        }

        if (rectChildren.Count == 0)
            return 0f;

        PlaceRow(axis, rowStart, rectChildren.Count, rowWidth, rowHeight, innerWidth, startY + y);
        return y + rowHeight;
    }

    private void PlaceRow(int axis, int start, int end, float rowWidth, float rowHeight, float innerWidth, float rowY)
    {
        if (axis < 0)
            return;

        float x = padding.left + (innerWidth - rowWidth) * GetAlignmentOnAxis(0);
        for (int i = start; i < end; i++)
        {
            RectTransform child = rectChildren[i];
            float childWidth = Mathf.Min(LayoutUtility.GetPreferredWidth(child), innerWidth);
            if (axis == 0)
            {
                SetChildAlongAxis(child, 0, x, childWidth);
            }
            else
            {
                float childHeight = LayoutUtility.GetPreferredHeight(child);
                SetChildAlongAxis(child, 1, rowY + (rowHeight - childHeight) * GetAlignmentOnAxis(1), childHeight);
            }

            x += childWidth + spacing.x;
        }
    }
}
