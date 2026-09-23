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

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target.activeSelf != isActive)
            target.SetActive(isActive);
    }
}
