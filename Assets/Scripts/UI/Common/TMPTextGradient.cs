using TMPro;
using UnityEngine;

/// <summary>
/// Spreads a horizontal gradient across the whole text block instead of per character
/// (TMP's built-in vertex gradient restarts on every glyph). The text colour still multiplies the result.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TMPTextGradient : MonoBehaviour
{
    [SerializeField] private Gradient gradient = new();

    private TMP_Text _text;

    public Gradient Gradient => gradient;

    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        _text.SetVerticesDirty();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
        if (_text != null)
            _text.SetVerticesDirty();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled && _text != null)
            _text.SetVerticesDirty();
    }

    public void SetGradient(Gradient newGradient)
    {
        if (newGradient == null || newGradient == gradient)
            return;

        gradient = newGradient;
        if (isActiveAndEnabled && _text != null)
            _text.SetVerticesDirty();
    }

    private void HandleTextChanged(Object changedText)
    {
        if (changedText == _text)
            ApplyGradient();
    }

    private void ApplyGradient()
    {
        TMP_TextInfo textInfo = _text.textInfo;
        int characterCount = textInfo.characterCount;
        if (characterCount == 0)
            return;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;

            minX = Mathf.Min(minX, character.bottomLeft.x);
            maxX = Mathf.Max(maxX, character.topRight.x);
        }

        if (maxX <= minX)
            return;

        float inverseWidth = 1f / (maxX - minX);
        Color tint = _text.color;
        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;

            TMP_MeshInfo meshInfo = textInfo.meshInfo[character.materialReferenceIndex];
            int vertexIndex = character.vertexIndex;
            for (int corner = 0; corner < 4; corner++)
            {
                float t = (meshInfo.vertices[vertexIndex + corner].x - minX) * inverseWidth;
                meshInfo.colors32[vertexIndex + corner] = gradient.Evaluate(t) * tint;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
