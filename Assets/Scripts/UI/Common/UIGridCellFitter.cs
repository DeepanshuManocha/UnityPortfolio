using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sizes a GridLayoutGroup's cells so a fixed number of columns x rows fills this rect (minus padding and
/// spacing). Give the rect a flexible height in its parent layout and the cells grow into any freed space,
/// e.g. when a row above is switched off. Runs as a layout controller, i.e. only during layout rebuilds,
/// before the grid places its children in the same pass.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(GridLayoutGroup))]
public class UIGridCellFitter : MonoBehaviour, ILayoutSelfController
{
    [SerializeField, Min(1)] private int columns = 2;
    [SerializeField, Min(1)] private int rows = 2;
    [Tooltip("Optional. Never shrink cells below this size.")]
    [SerializeField] private Vector2 minCellSize = new(200f, 120f);

    private GridLayoutGroup _grid;
    private RectTransform _rect;

    public int Columns => columns;
    public int Rows => rows;

    private void OnEnable()
    {
        Fit();
    }

    public void SetLayoutHorizontal()
    {
        Fit();
    }

    public void SetLayoutVertical()
    {
        Fit();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled)
            Fit();
    }

    public void Fit()
    {
        if (_grid == null)
            _grid = GetComponent<GridLayoutGroup>();
        if (_rect == null)
            _rect = (RectTransform)transform;

        Rect area = _rect.rect;
        if (area.width <= 0f || area.height <= 0f)
            return;

        RectOffset padding = _grid.padding;
        Vector2 spacing = _grid.spacing;
        float width = (area.width - padding.horizontal - spacing.x * (columns - 1)) / columns;
        float height = (area.height - padding.vertical - spacing.y * (rows - 1)) / rows;

        // The grid's setter only marks the layout dirty when the size actually changes.
        _grid.cellSize = new Vector2(Mathf.Max(minCellSize.x, width), Mathf.Max(minCellSize.y, height));
    }
}
