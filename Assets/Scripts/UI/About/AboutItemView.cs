using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Shared view for skill pills and focus cards. Leave Subtitle empty on prefabs that don't show one.</summary>
[DisallowMultipleComponent]
public class AboutItemView : MonoBehaviour, IUIItemView<AboutItem>
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image iconImage;
    [Tooltip("Graphics tinted with the item's icon colour. Each keeps its authored alpha.")]
    [SerializeField, FormerlySerializedAs("accentGraphics")] private Graphic[] iconGraphics;
    [Tooltip("Borders and glows tinted with the item's outline colour. Each keeps its authored alpha.")]
    [SerializeField] private Graphic[] outlineGraphics;

    private float[] _iconBaseAlphas;
    private float[] _outlineBaseAlphas;

    public void Bind(AboutItem item)
    {
        UIBinding.SetText(titleText, item.Title);
        UIBinding.SetText(subtitleText, item.Subtitle);
        UIBinding.SetIcon(iconImage, item.Icon);
        UIBinding.SetTints(iconGraphics, ref _iconBaseAlphas, item.IconColor);
        UIBinding.SetTints(outlineGraphics, ref _outlineBaseAlphas, item.OutlineColor);
    }
}
