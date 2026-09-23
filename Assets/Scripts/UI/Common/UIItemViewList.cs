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

    public void Bind(IReadOnlyList<TData> items)
    {
        if (!IsValid)
            return;

        AdoptExistingViews();

        int count = items?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            TView view = i < _views.Count ? _views[i] : CreateView();
            SetActive(view, true);
            view.Bind(items[i]);
        }

        for (int i = count; i < _views.Count; i++)
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
