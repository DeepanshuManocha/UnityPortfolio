using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Slideshow Image Collection",
    menuName = "Portfolio/UI/Slideshow Image Collection")]
public sealed class SlideshowImageCollection : ScriptableObject
{
    [SerializeField] private List<Sprite> images = new();

    public IReadOnlyList<Sprite> Images => images;
    public int Count => images?.Count ?? 0;

    public Sprite GetImage(int index)
    {
        return index >= 0 && index < Count ? images[index] : null;
    }
}
