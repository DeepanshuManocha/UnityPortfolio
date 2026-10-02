using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UILabelChip
{
    [SerializeField] private string label;
    [SerializeField] private Color color = Color.white;

    public string Label => label;
    public Color Color => color;

    public UILabelChip()
    {
    }

    public UILabelChip(string label, Color color)
    {
        this.label = label;
        this.color = color;
    }
}

/// <summary>Small coloured label, e.g. a project tag.</summary>
[DisallowMultipleComponent]
public class UILabelChipView : MonoBehaviour, IUIItemView<UILabelChip>
{
    [SerializeField] private TMP_Text labelText;
    [Tooltip("Borders and fills tinted with the chip colour. Each keeps its authored alpha.")]
    [SerializeField] private Graphic[] tintGraphics;

    private float[] _tintBaseAlphas;

    // Authored size, captured before the first rescale.
    private bool _hasRestingSize;
    private float _restingFontSize;
    private float _restingHeight;
    private RectOffset _restingPadding;
    private LayoutElement _layoutElement;
    private HorizontalOrVerticalLayoutGroup _layoutGroup;
    private float _scale = 1f;

    public void Bind(UILabelChip chip)
    {
        UIBinding.SetText(labelText, chip.Label);
        UIBinding.SetTints(tintGraphics, ref _tintBaseAlphas, chip.Color);
    }

    /// <summary>
    /// Resizes the chip through its font, height and padding (not localScale), so layout groups
    /// account for the new size. 1 = authored size.
    /// </summary>
    public void SetScale(float scale)
    {
        if (Mathf.Approximately(scale, _scale))
            return;

        CaptureRestingSize();
        _scale = scale;

        if (labelText != null)
            labelText.fontSize = _restingFontSize * scale;

        if (_layoutElement != null)
        {
            _layoutElement.minHeight = _restingHeight * scale;
            _layoutElement.preferredHeight = _restingHeight * scale;
        }

        if (_layoutGroup != null)
        {
            _layoutGroup.padding = new RectOffset(
                Mathf.RoundToInt(_restingPadding.left * scale), Mathf.RoundToInt(_restingPadding.right * scale),
                Mathf.RoundToInt(_restingPadding.top * scale), Mathf.RoundToInt(_restingPadding.bottom * scale));
        }
    }

    /// <summary>Width the chip would have at <paramref name="scale"/> for its current label (no layout pass needed).</summary>
    public float GetPreferredWidth(float scale)
    {
        CaptureRestingSize();
        if (labelText == null || _scale <= 0f)
            return 0f;

        // Measured at the current font size, then brought back to the authored size.
        float restingLabelWidth = labelText.GetPreferredValues(labelText.text).x / _scale;
        float restingPadding = _restingPadding != null ? _restingPadding.left + _restingPadding.right : 0f;
        return (restingLabelWidth + restingPadding) * scale;
    }

    private void CaptureRestingSize()
    {
        if (_hasRestingSize)
            return;

        _hasRestingSize = true;
        _restingFontSize = labelText != null ? labelText.fontSize : 0f;
        if (TryGetComponent(out _layoutElement))
            _restingHeight = _layoutElement.preferredHeight;
        if (TryGetComponent(out _layoutGroup))
            _restingPadding = new RectOffset(_layoutGroup.padding.left, _layoutGroup.padding.right,
                _layoutGroup.padding.top, _layoutGroup.padding.bottom);
    }
}
