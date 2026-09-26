using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Binds <see cref="ProjectsSectionData"/> to the Projects UI and owns the filter + page state.
/// All working lists are reused, so filtering and paging don't allocate after warm-up.
/// </summary>
[DisallowMultipleComponent]
public class ProjectsSectionView : MonoBehaviour
{
    private const int AllFilterIndex = 0;

    [Header("Content")]
    [SerializeField] private ProjectsSectionData data;

    [Header("Header")]
    [SerializeField] private TMP_Text titleLeadText;
    [SerializeField] private TMP_Text titleHighlightText;
    [Tooltip("Optional. Receives the title gradient from the data asset.")]
    [SerializeField] private TMPTextGradient titleHighlightGradient;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Filters")]
    [SerializeField] private UIItemViewList<UIOptionModel, UIOptionButtonView> filters;

    [Header("Projects")]
    [SerializeField] private UIItemViewList<ProjectCardModel, ProjectCardView> cards;
    [Tooltip("Shown instead of the cards when the selected filter has no projects.")]
    [SerializeField] private TMP_Text emptyStateText;
    [Tooltip("Faded out and back in when the page or filter changes.")]
    [SerializeField] private CanvasGroup cardsGroup;

    [Header("Pagination")]
    [Tooltip("Hidden when everything fits on one page.")]
    [SerializeField] private GameObject paginationRoot;
    [SerializeField] private UIItemViewList<UIOptionModel, UIOptionButtonView> pages;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Color pageAccentColor = new(0.23f, 0.51f, 0.96f, 1f);

    [Header("Page Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.3f;
    [SerializeField] private bool useUnscaledTime;

    [Header("Events")]
    [SerializeField] private UnityEvent<ProjectData> projectSelected;

    private readonly List<ProjectData> _filteredProjects = new();
    private readonly List<UIOptionModel> _filterModels = new();
    private readonly List<ProjectCardModel> _cardModels = new();
    private readonly List<UIOptionModel> _pageModels = new();

    private Action<int> _filterClicked;
    private Action<int> _pageClicked;
    private Action<ProjectData> _cardSelected;
    private TweenCallback _bindCards;
    private TweenCallback _clearTransition;

    private ProjectsSectionData _boundData;
    private Sequence _transition;
    private int _filterIndex = AllFilterIndex;
    private int _pageIndex;
    private int _pageCount = 1;

    public ProjectsSectionData Data => data;
    public ProjectCategory SelectedCategory => GetCategory(_filterIndex);
    public int PageIndex => _pageIndex;
    public int PageCount => _pageCount;
    public UnityEvent<ProjectData> ProjectSelected => projectSelected;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PreviousPage);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);
    }

    private void OnEnable()
    {
        SubscribeToData();
        if (_boundData != data)
            Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromData();
        KillTransition();
    }

    public void SetData(ProjectsSectionData newData)
    {
        if (newData == data)
            return;

        UnsubscribeFromData();
        data = newData;
        SubscribeToData();
        _filterIndex = AllFilterIndex;
        _pageIndex = 0;
        Refresh();
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        _boundData = data;
        if (data == null)
        {
            Debug.LogWarning($"{nameof(ProjectsSectionView)} on '{name}' has no {nameof(ProjectsSectionData)}.", this);
            return;
        }

        EnsureCallbacks();
        KillTransition();

        UIBinding.SetText(titleLeadText, data.TitleLead);
        UIBinding.SetText(titleHighlightText, data.TitleHighlight);
        UIBinding.SetText(subtitleText, data.Subtitle);
        if (titleHighlightGradient != null)
            titleHighlightGradient.SetGradient(data.TitleGradient);

        _filterIndex = Mathf.Clamp(_filterIndex, AllFilterIndex, data.Categories.Count);
        RebuildFilteredProjects();
        BindFilters();
        BindPagination();
        BindCards();
    }

    /// <param name="filterIndex">0 = All, then one per category in data order.</param>
    public void SelectFilter(int filterIndex)
    {
        if (data == null || filterIndex == _filterIndex || filterIndex < AllFilterIndex || filterIndex > data.Categories.Count)
            return;

        _filterIndex = filterIndex;
        _pageIndex = 0;
        RebuildFilteredProjects();
        BindFilters();
        TransitionToPage(0);
    }

    public void ShowPage(int pageIndex)
    {
        if (data == null || pageIndex == _pageIndex || pageIndex < 0 || pageIndex >= _pageCount)
            return;

        TransitionToPage(pageIndex);
    }

    public void NextPage()
    {
        ShowPage(_pageIndex + 1);
    }

    public void PreviousPage()
    {
        ShowPage(_pageIndex - 1);
    }

    private void EnsureCallbacks()
    {
        _filterClicked ??= SelectFilter;
        _pageClicked ??= ShowPage;
        _cardSelected ??= HandleCardSelected;
        _bindCards ??= BindCards;
        _clearTransition ??= () => _transition = null;
    }

    private void TransitionToPage(int pageIndex)
    {
        _pageIndex = pageIndex;
        BindPagination();
        KillTransition();

        if (cardsGroup == null || transitionDuration <= 0f || !Application.isPlaying)
        {
            BindCards();
            return;
        }

        float halfDuration = transitionDuration * 0.5f;
        // Lifetime is managed by KillTransition (not SetLink) so an interrupted fade still binds its cards.
        _transition = DOTween.Sequence().SetUpdate(useUnscaledTime);
        _transition.Append(cardsGroup.DOFade(0f, halfDuration).SetEase(Ease.InQuad));
        _transition.AppendCallback(_bindCards);
        _transition.Append(cardsGroup.DOFade(1f, halfDuration).SetEase(Ease.OutQuad));
        _transition.OnComplete(_clearTransition);
    }

    private void KillTransition()
    {
        if (_transition == null)
            return;

        // Completing (rather than just killing) runs the pending card bind and restores full alpha.
        Sequence transition = _transition;
        _transition = null;
        transition.Kill(true);
        if (cardsGroup != null)
            cardsGroup.alpha = 1f;
    }

    private void RebuildFilteredProjects()
    {
        _filteredProjects.Clear();
        ProjectCategory category = GetCategory(_filterIndex);
        IReadOnlyList<ProjectData> projects = data.Projects;
        for (int i = 0; i < projects.Count; i++)
        {
            ProjectData project = projects[i];
            if (IsVisible(project) && project.IsInCategory(category))
                _filteredProjects.Add(project);
        }

        int perPage = Mathf.Max(1, data.ProjectsPerPage);
        _pageCount = Mathf.Max(1, Mathf.CeilToInt(_filteredProjects.Count / (float)perPage));
        _pageIndex = Mathf.Clamp(_pageIndex, 0, _pageCount - 1);
    }

    private void BindFilters()
    {
        _filterModels.Clear();
        _filterModels.Add(CreateFilterModel(AllFilterIndex, data.AllLabel, data.AllColor, null));

        IReadOnlyList<ProjectCategory> categories = data.Categories;
        for (int i = 0; i < categories.Count; i++)
        {
            ProjectCategory category = categories[i];
            if (category != null)
                _filterModels.Add(CreateFilterModel(i + 1, category.DisplayName, category.Color, category));
        }

        filters.Bind(_filterModels);
    }

    private UIOptionModel CreateFilterModel(int index, string label, Color color, ProjectCategory category)
    {
        string text = string.Format(data.FilterLabelFormat, label, CountProjects(category));
        return new UIOptionModel(index, text, color, index == _filterIndex, _filterClicked);
    }

    private void BindCards()
    {
        _cardModels.Clear();
        int perPage = Mathf.Max(1, data.ProjectsPerPage);
        int start = _pageIndex * perPage;
        int end = Mathf.Min(start + perPage, _filteredProjects.Count);
        for (int i = start; i < end; i++)
            _cardModels.Add(new ProjectCardModel(_filteredProjects[i], _cardSelected));

        cards.Bind(_cardModels);
        UIBinding.SetText(emptyStateText, _filteredProjects.Count == 0 ? data.EmptyMessage : null);
    }

    private void BindPagination()
    {
        bool hasMultiplePages = _pageCount > 1;
        SetPaginationVisible(hasMultiplePages);
        if (!hasMultiplePages)
            return;

        _pageModels.Clear();
        for (int i = 0; i < _pageCount; i++)
            _pageModels.Add(new UIOptionModel(i, (i + 1).ToString(), pageAccentColor, i == _pageIndex, _pageClicked));

        pages.Bind(_pageModels);

        if (previousButton != null)
            previousButton.interactable = _pageIndex > 0;
        if (nextButton != null)
            nextButton.interactable = _pageIndex < _pageCount - 1;
    }

    private void SetPaginationVisible(bool isVisible)
    {
        if (paginationRoot != null)
        {
            SetActive(paginationRoot, isVisible);
            return;
        }

        // No root assigned: hide the individual pieces instead.
        if (previousButton != null)
            SetActive(previousButton.gameObject, isVisible);
        if (nextButton != null)
            SetActive(nextButton.gameObject, isVisible);
        if (pages.Container != null)
            SetActive(pages.Container.gameObject, isVisible);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target.activeSelf != isActive)
            target.SetActive(isActive);
    }

    private int CountProjects(ProjectCategory category)
    {
        int count = 0;
        IReadOnlyList<ProjectData> projects = data.Projects;
        for (int i = 0; i < projects.Count; i++)
        {
            if (IsVisible(projects[i]) && projects[i].IsInCategory(category))
                count++;
        }

        return count;
    }

    private ProjectCategory GetCategory(int filterIndex)
    {
        if (data == null || filterIndex <= AllFilterIndex || filterIndex > data.Categories.Count)
            return null;

        return data.Categories[filterIndex - 1];
    }

    private void HandleCardSelected(ProjectData project)
    {
        projectSelected?.Invoke(project);
    }

    private static bool IsVisible(ProjectData project)
    {
        return project != null && !project.IsHidden;
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
