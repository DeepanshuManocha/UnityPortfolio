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

    public void Bind(UILabelChip chip)
    {
        UIBinding.SetText(labelText, chip.Label);
        UIBinding.SetTints(tintGraphics, ref _tintBaseAlphas, chip.Color);
    }
}
