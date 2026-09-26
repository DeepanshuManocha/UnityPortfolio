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

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target.activeSelf != isActive)
            target.SetActive(isActive);
    }
}
