using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds a data list to pooled views under a container. Views are reused on rebind and surplus views are
/// deactivated instead of destroyed. Views already under the container (edit-time previews or a template child)
/// are adopted on first bind, so they are reused rather than duplicated.
/// </summary>
[Serializable]
public class UIItemViewList<TData, TView> where TView : Component, IUIItemView<TData>
{
    [SerializeField] private TView prefab;
    [SerializeField] private RectTransform container;

    private readonly List<TView> _views = new();
    private bool _hasAdoptedExistingViews;

    public IReadOnlyList<TView> Views => _views;
    public RectTransform Container => container;
    public bool IsValid => prefab != null && container != null;

    /// <param name="include">Optional filter; items it rejects get no view. Pass a cached delegate to avoid allocations.</param>
    public void Bind(IReadOnlyList<TData> items, Func<TData, bool> include = null)
    {
        if (!IsValid)
            return;

        AdoptExistingViews();

        int visibleCount = 0;
        int count = items?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            TData item = items[i];
            if (include != null && !include(item))
                continue;

            TView view = visibleCount < _views.Count ? _views[visibleCount] : CreateView();
            SetActive(view, true);
            view.Bind(item);
            visibleCount++;
        }

        for (int i = visibleCount; i < _views.Count; i++)
            SetActive(_views[i], false);
    }

    private void AdoptExistingViews()
    {
        if (_hasAdoptedExistingViews)
            return;

        _hasAdoptedExistingViews = true;
        for (int i = 0; i < container.childCount; i++)
        {
            if (container.GetChild(i).TryGetComponent(out TView view))
                _views.Add(view);
        }
    }

    private TView CreateView()
    {
        TView view = UnityEngine.Object.Instantiate(prefab, container, false);
        _views.Add(view);
        return view;
    }

    private static void SetActive(TView view, bool isActive)
    {
        if (view.gameObject.activeSelf != isActive)
            view.gameObject.SetActive(isActive);
    }
}
