using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OnboardingStep
{
    [Tooltip("Index in the CameraSwitcher's camera list. The step shows while that camera is active.")]
    [SerializeField, Min(0)] private int cameraIndex;
    [SerializeField] private string titleLead;
    [Tooltip("Optional gradient part after the lead, e.g. \"Projects\" in \"About Me & Projects\".")]
    [SerializeField] private string titleHighlight;
    [SerializeField, TextArea(2, 4)] private string description;

    [Header("Instruction (optional)")]
    [Tooltip("Leave empty to hide the whole instruction row; the panel shrinks to fit.")]
    [SerializeField, TextArea(1, 3)] private string instruction;
    [Tooltip("Optional word in front of the instruction, drawn in the highlight colour (e.g. \"Scroll\").")]
    [SerializeField] private string instructionHighlight;
    [SerializeField] private Sprite instructionIcon;

    public int CameraIndex => cameraIndex;
    public string TitleLead => titleLead;
    public string TitleHighlight => titleHighlight;
    public string Description => description;
    public string Instruction => instruction;
    public string InstructionHighlight => instructionHighlight;
    public Sprite InstructionIcon => instructionIcon;
    public bool HasInstruction => !string.IsNullOrEmpty(instruction) || !string.IsNullOrEmpty(instructionHighlight);
}

[CreateAssetMenu(
    fileName = "Onboarding Tour",
    menuName = "Portfolio/UI/Onboarding Tour")]
public class OnboardingTourData : ScriptableObject
{
    [Header("Labels")]
    [SerializeField] private string eyebrow = "PORTFOLIO TOUR";

    [Header("Style")]
    [SerializeField] private Gradient titleHighlightGradient = new();
    [SerializeField] private Color instructionHighlightColor = new(0.24f, 0.85f, 1f, 1f);

    [Header("Steps (order = tour order; moving on from the last one ends the tour)")]
    [SerializeField] private List<OnboardingStep> steps = new();

    public string Eyebrow => eyebrow;
    public Gradient TitleHighlightGradient => titleHighlightGradient;
    public Color InstructionHighlightColor => instructionHighlightColor;
    public IReadOnlyList<OnboardingStep> Steps => steps;

    /// <summary>Index of the step shown for this camera, or -1.</summary>
    public int FindStepForCamera(int cameraIndex)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].CameraIndex == cameraIndex)
                return i;
        }

        return -1;
    }

#if UNITY_EDITOR
    /// <summary>Editor only: raised after inspector edits so the tour can refresh in Play Mode.</summary>
    public event Action Changed;

    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= RaiseChanged;
        UnityEditor.EditorApplication.delayCall += RaiseChanged;
    }

    private void RaiseChanged()
    {
        if (this != null)
            Changed?.Invoke();
    }
#endif
}
