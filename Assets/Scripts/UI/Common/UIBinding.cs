using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIBinding
{
    /// <summary>Assigns the text and hides the label's GameObject when the value is empty.</summary>
    public static void SetText(TMP_Text label, string value)
    {
        if (label == null)
            return;

        bool hasValue = !string.IsNullOrEmpty(value);
        SetActive(label.gameObject, hasValue);
        if (hasValue)
            label.text = value;
    }

    /// <summary>Assigns the sprite and hides the image's GameObject when there is none, so layouts collapse.</summary>
    public static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        bool hasSprite = sprite != null;
        SetActive(image.gameObject, hasSprite);
        if (hasSprite)
            image.sprite = sprite;
    }

    /// <summary>
    /// Assigns the sprite and disables the Image (keeping its layout slot) when there is none. With an
    /// Envelope Parent <paramref name="fitter"/>, the image fills and crops to its masked frame at any aspect.
    /// </summary>
    public static void SetSprite(Image image, Sprite sprite, AspectRatioFitter fitter = null)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;

        if (sprite != null && fitter != null)
        {
            Rect rect = sprite.rect;
            fitter.aspectRatio = rect.width / Mathf.Max(1f, rect.height);
        }
    }

    /// <summary>
    /// Makes the target invisible and unclickable through a CanvasGroup (added on first use) while it stays
    /// active, so it keeps its place in layout groups.
    /// </summary>
    public static void SetConcealed(Component target, bool isConcealed)
    {
        if (target == null)
            return;

        if (!target.TryGetComponent(out CanvasGroup group))
        {
            if (!isConcealed)
                return;
            group = target.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = isConcealed ? 0f : 1f;
        group.interactable = !isConcealed;
        group.blocksRaycasts = !isConcealed;
    }

    public static void SetActive(Component target, bool isActive)
    {
        if (target != null)
            SetActive(target.gameObject, isActive);
    }

    /// <summary>Applies the tint's RGB and multiplies its alpha with the graphic's authored alpha.</summary>
    public static void SetTint(Graphic graphic, Color tint, float baseAlpha)
    {
        if (graphic == null)
            return;

        tint.a *= baseAlpha;
        graphic.color = tint;
    }

    /// <summary>
    /// Tints every graphic, keeping each one's authored alpha. The alphas are captured into
    /// <paramref name="baseAlphas"/> on the first call, so re-tinting never compounds.
    /// </summary>
    public static void SetTints(Graphic[] graphics, ref float[] baseAlphas, Color tint)
    {
        if (graphics == null)
            return;

        if (baseAlphas == null)
        {
            baseAlphas = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
                baseAlphas[i] = graphics[i] != null ? graphics[i].color.a : 1f;
        }

        for (int i = 0; i < graphics.Length; i++)
            SetTint(graphics[i], tint, baseAlphas[i]);
    }

    /// <summary>SetActive that skips the call (and the layout rebuild it triggers) when nothing changes.</summary>
    public static void SetActive(GameObject target, bool isActive)
    {
        if (target != null && target.activeSelf != isActive)
            target.SetActive(isActive);
    }
}
