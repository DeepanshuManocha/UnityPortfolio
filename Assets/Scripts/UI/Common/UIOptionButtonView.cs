using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public readonly struct UIOptionModel
{
    public readonly int Index;
    public readonly string Label;
    public readonly Color AccentColor;
    public readonly bool IsSelected;
    public readonly Action<int> OnClick;

    public UIOptionModel(int index, string label, Color accentColor, bool isSelected, Action<int> onClick)
    {
        Index = index;
        Label = label;
        AccentColor = accentColor;
        IsSelected = isSelected;
        OnClick = onClick;
    }
}

/// <summary>
/// A clickable, selectable option such as a filter tab or a page number. Reports its index back through
/// the model's callback, so one cached delegate can serve every option in a list.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIOptionButtonView : MonoBehaviour, IUIItemView<UIOptionModel>
{
    [SerializeField] private TMP_Text labelText;
    [Tooltip("Tinted with the accent colour. Each keeps its authored alpha.")]
    [SerializeField] private Graphic[] accentGraphics;
    [Tooltip("Only active while this option is selected (e.g. a filled background).")]
    [SerializeField] private GameObject selectedState;
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color selectedLabelColor = Color.white;

    private Button _button;
    private float[] _accentBaseAlphas;
    private UIOptionModel _model;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(UIOptionModel model)
    {
        _model = model;
        UIBinding.SetText(labelText, model.Label);
        if (labelText != null)
            labelText.color = model.IsSelected ? selectedLabelColor : labelColor;

        if (selectedState != null && selectedState.activeSelf != model.IsSelected)
            selectedState.SetActive(model.IsSelected);

        UIBinding.SetTints(accentGraphics, ref _accentBaseAlphas, model.AccentColor);
    }

    private void HandleClick()
    {
        _model.OnClick?.Invoke(_model.Index);
    }
}
