using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SlideshowImage
{
    [SerializeField] private Sprite image;
    [Tooltip("Index into the collection's Headers; -1 = no header.")]
    [SerializeField] private int header = -1;

    public Sprite Image => image;
    public int Header => header;
}

[CreateAssetMenu(
    fileName = "Slideshow Image Collection",
    menuName = "Portfolio/UI/Slideshow Image Collection")]
public sealed class SlideshowImageCollection : ScriptableObject
{
    [Tooltip("Optional header titles. Each image picks one of these; the matching header highlights while it shows.")]
    [SerializeField] private List<string> headers = new();
    [Tooltip("Playback order. Each image can use any header, in any order.")]
    [SerializeField] private List<SlideshowImage> images = new();

    public IReadOnlyList<string> Headers => headers;
    public IReadOnlyList<SlideshowImage> Images => images;
    public int Count => images?.Count ?? 0;

    public Sprite GetImage(int index)
    {
        return index >= 0 && index < Count ? images[index]?.Image : null;
    }

    /// <summary>Header of the image at <paramref name="index"/>, or -1 if it has none.</summary>
    public int GetHeaderIndex(int index)
    {
        if (index < 0 || index >= Count || images[index] == null)
            return -1;

        int header = images[index].Header;
        return header >= 0 && header < headers.Count ? header : -1;
    }
}
