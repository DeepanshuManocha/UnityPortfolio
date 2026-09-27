using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TechStackItemView : MonoBehaviour, IUIItemView<TechStackItem>
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;

    public void Bind(TechStackItem item)
    {
        UIBinding.SetIcon(iconImage, item != null ? item.Icon : null);
        UIBinding.SetText(labelText, item != null ? item.DisplayName : null);
    }
}
