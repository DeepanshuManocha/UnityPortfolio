using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared view for skill pills and focus cards. Leave Subtitle empty on prefabs that don't show one.</summary>
[DisallowMultipleComponent]
public class AboutItemView : MonoBehaviour, IUIItemView<AboutItem>
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image iconImage;
    [Tooltip("Borders, glows and icons tinted with the item's accent. Each keeps its authored alpha.")]
    [SerializeField] private Graphic[] accentGraphics;

    private float[] _accentBaseAlphas;

    public void Bind(AboutItem item)
    {
        UIBinding.SetText(titleText, item.Title);
        UIBinding.SetText(subtitleText, item.Subtitle);
        UIBinding.SetIcon(iconImage, item.Icon);
        ApplyAccent(item.AccentColor);
    }

    private void ApplyAccent(Color accent)
    {
        if (accentGraphics == null)
            return;

        CacheAccentAlphas();
        for (int i = 0; i < accentGraphics.Length; i++)
            UIBinding.SetTint(accentGraphics[i], accent, _accentBaseAlphas[i]);
    }

    private void CacheAccentAlphas()
    {
        if (_accentBaseAlphas != null)
            return;

        _accentBaseAlphas = new float[accentGraphics.Length];
        for (int i = 0; i < accentGraphics.Length; i++)
            _accentBaseAlphas[i] = accentGraphics[i] != null ? accentGraphics[i].color.a : 1f;
    }
}
