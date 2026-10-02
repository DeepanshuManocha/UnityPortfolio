using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasWidthAndHeightController : MonoBehaviour
{
    public static event Action OnResolutionChanged;

    private CanvasScaler _canvasScaler;
    
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    void Start()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        
        UpdateCanvasScaler();
        OnResolutionChanged?.Invoke();
    }

    void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            UpdateCanvasScaler();
        }
    }

    public void UpdateCanvasScaler()
    {
        if (_canvasScaler == null) return;

        float value = CalculateMatchFloatValue();
        _canvasScaler.matchWidthOrHeight = value;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    public float CalculateMatchFloatValue()
    {
        float value = (float)Screen.width / Screen.height;
        value -= 1f;

        // Used 0.35f for better results on Ipad and almost square res
        // Used 0.85f for better results on stretched res
        if (value < 0.35f)
            value = 0f;
        else if (value > 0.85f)
            value = 1f;

        return value;
    }
}