using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public readonly struct QuickLinkModel
{
    public readonly QuickLink Link;
    public readonly Action<QuickLink> OnClick;
    /// <summary>Invisible and unclickable, but still takes its place in the column.</summary>
    public readonly bool IsConcealed;

    public QuickLinkModel(QuickLink link, Action<QuickLink> onClick, bool isConcealed)
    {
        Link = link;
        OnClick = onClick;
        IsConcealed = isConcealed;
    }
}

/// <summary>
/// Round icon button that grows a labelled pill on hover. The pill grows away from its pivot:
/// pivot X = 1 grows to the left (for a right-edge panel), pivot X = 0 grows to the right.
/// Web links are armed on pointer-down so WebGL opens them inside the browser's click.
/// Lives on the same object as the Button, so it receives the pointer-down before the Button swallows it.
/// </summary>
[DisallowMultipleComponent]
public class QuickLinkButtonView : MonoBehaviour, IUIItemView<QuickLinkModel>, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private Button button;
    [Tooltip("Optional. Used to conceal the button without collapsing the layout; added at runtime if empty.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [Tooltip("Only active for links that ask for a divider above them.")]
    [SerializeField] private GameObject separator;
    [Tooltip("Grows by Separator Space when the divider is shown, so the list leaves room for it.")]
    [SerializeField] private LayoutElement layoutElement;
    [Tooltip("Circle size: the pill's collapsed width and the button's height without a divider.")]
    [SerializeField, Min(1f)] private float buttonSize = 88f;
    [SerializeField, Min(0f)] private float separatorSpace = 40f;

    [Header("Hover Label")]
    [Tooltip("Pill behind the circle; its width animates on hover.")]
    [SerializeField] private RectTransform expander;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private CanvasGroup labelGroup;
    [Tooltip("Space between the label and the pill's outer edge.")]
    [SerializeField, Min(0f)] private float labelPadding = 28f;
    [Tooltip("Space between the label and the circle.")]
    [SerializeField, Min(0f)] private float labelGap = 14f;
    [SerializeField, Min(0f)] private float expandDuration = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    private QuickLinkModel _model;
    private Sequence _hoverTween;
    private float _expandedWidth;

    private bool _isArmedForRelease;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
        _hoverTween?.Kill();
    }

    private void OnDisable()
    {
        _hoverTween?.Kill();
        _hoverTween = null;
        SetExpanded(false, true);
    }

    public void Bind(QuickLinkModel model)
    {
        _model = model;
        QuickLink link = model.Link;
        UIBinding.SetIcon(iconImage, link?.Icon);
        bool hasSeparator = link != null && link.SeparatorBefore;
        UIBinding.SetActive(separator, hasSeparator);
        if (layoutElement != null)
        {
            float height = buttonSize + (hasSeparator ? separatorSpace : 0f);
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        SetConcealed(model.IsConcealed);

        string label = link?.Label;
        if (labelText != null)
            labelText.text = label ?? string.Empty;

        // Width is measured once per bind, not per hover.
        float labelWidth = labelText != null && !string.IsNullOrEmpty(label) ? labelText.GetPreferredValues(label).x : 0f;
        _expandedWidth = labelWidth > 0f ? buttonSize + labelGap + labelWidth + labelPadding : buttonSize;
        SetExpanded(false, true);
    }

    private void SetConcealed(bool isConcealed)
    {
        if (canvasGroup == null)
        {
            if (!isConcealed)
                return;
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = isConcealed ? 0f : 1f;
        canvasGroup.interactable = !isConcealed;
        canvasGroup.blocksRaycasts = !isConcealed;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetExpanded(true, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetExpanded(false, false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        QuickLink link = _model.Link;
        _isArmedForRelease = link != null && link.IsWebLink && (button == null || button.interactable) &&
                             WebLinkOpener.ArmOnPointerDown(link.ResolveUrl(), link.Mode);
    }

    private void HandleClick()
    {
        // WebGL already opens armed links on release; everything else goes through the owner.
        if (_isArmedForRelease)
        {
            _isArmedForRelease = false;
            return;
        }

        if (_model.Link != null)
            _model.OnClick?.Invoke(_model.Link);
    }

    private void SetExpanded(bool isExpanded, bool immediate)
    {
        if (expander == null)
            return;

        _hoverTween?.Kill();
        _hoverTween = null;
        float width = isExpanded ? _expandedWidth : buttonSize;
        float alpha = isExpanded ? 1f : 0f;

        if (immediate || expandDuration <= 0f)
        {
            expander.sizeDelta = new Vector2(width, expander.sizeDelta.y);
            if (labelGroup != null)
                labelGroup.alpha = alpha;
            return;
        }

        _hoverTween = DOTween.Sequence().SetUpdate(useUnscaledTime).SetLink(gameObject);
        _hoverTween.Join(expander.DOSizeDelta(new Vector2(width, expander.sizeDelta.y), expandDuration)
            .SetEase(isExpanded ? Ease.OutCubic : Ease.InCubic));
        if (labelGroup != null)
        {
            // Label fades in after the pill has started opening, and out quickly when closing.
            _hoverTween.Join(labelGroup.DOFade(alpha, expandDuration * (isExpanded ? 0.8f : 0.5f))
                .SetDelay(isExpanded ? expandDuration * 0.2f : 0f));
        }
    }
}
