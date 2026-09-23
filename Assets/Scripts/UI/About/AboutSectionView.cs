using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AboutSectionView : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private AboutSectionData data;

    [Header("Intro")]
    [SerializeField] private TMP_Text eyebrowText;
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Optional. Receives the name gradient from the data asset.")]
    [SerializeField] private TMPTextGradient nameGradient;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text bioText;
    [SerializeField] private TMP_Text taglineText;
    [SerializeField] private Image backgroundImage;

    [Header("Industries")]
    [SerializeField] private TMP_Text industriesTitleText;
    [SerializeField] private Image industriesIconImage;
    [SerializeField, FormerlySerializedAs("skills")] private UIItemViewList<AboutItem, AboutItemView> industries;

    [Header("Focus")]
    [SerializeField] private TMP_Text focusTitleText;
    [SerializeField] private Image focusIconImage;
    [SerializeField] private UIItemViewList<AboutItem, AboutItemView> focusItems;

    private AboutSectionData _boundData;

    public AboutSectionData Data => data;

    private void OnEnable()
    {
        SubscribeToData();
        if (_boundData != data)
            Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromData();
    }

    public void SetData(AboutSectionData newData)
    {
        if (newData == data)
            return;

        UnsubscribeFromData();
        data = newData;
        SubscribeToData();
        Refresh();
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        _boundData = data;
        if (data == null)
        {
            Debug.LogWarning($"{nameof(AboutSectionView)} on '{name}' has no {nameof(AboutSectionData)}.", this);
            return;
        }

        UIBinding.SetText(eyebrowText, data.Eyebrow);
        UIBinding.SetText(greetingText, data.Greeting);
        UIBinding.SetText(nameText, data.DisplayName);
        UIBinding.SetText(roleText, data.Role);
        UIBinding.SetText(bioText, data.Bio);
        UIBinding.SetText(taglineText, data.Tagline);
        UIBinding.SetText(industriesTitleText, data.IndustriesTitle);
        UIBinding.SetIcon(industriesIconImage, data.IndustriesIcon);
        UIBinding.SetText(focusTitleText, data.FocusTitle);
        UIBinding.SetIcon(focusIconImage, data.FocusIcon);

        if (roleText != null)
            roleText.color = data.RoleColor;

        if (nameGradient != null)
            nameGradient.SetGradient(data.NameGradient);

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data.Background;
            backgroundImage.enabled = data.Background != null;
        }

        industries.Bind(data.Industries);
        focusItems.Bind(data.FocusItems);
    }

    private void SubscribeToData()
    {
#if UNITY_EDITOR
        if (data != null)
            data.Changed += Refresh;
#endif
    }

    private void UnsubscribeFromData()
    {
#if UNITY_EDITOR
        if (data != null)
            data.Changed -= Refresh;
#endif
    }
}
