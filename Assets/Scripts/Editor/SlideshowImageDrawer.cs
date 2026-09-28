using UnityEditor;
using UnityEngine;

/// <summary>Draws a slideshow image as one row: sprite field + a dropdown of the collection's header titles.</summary>
[CustomPropertyDrawer(typeof(SlideshowImage))]
public class SlideshowImageDrawer : PropertyDrawer
{
    private const float Spacing = 6f;
    private const string NoHeader = "(No header)";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty image = property.FindPropertyRelative("image");
        SerializedProperty header = property.FindPropertyRelative("header");

        EditorGUI.BeginProperty(position, label, property);
        position.height = EditorGUIUtility.singleLineHeight;

        float half = (position.width - Spacing) * 0.5f;
        var imageRect = new Rect(position.x, position.y, half, position.height);
        var headerRect = new Rect(imageRect.xMax + Spacing, position.y, half, position.height);

        EditorGUI.PropertyField(imageRect, image, GUIContent.none);
        header.intValue = DrawHeaderPopup(headerRect, header.intValue, property.serializedObject.FindProperty("headers"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static int DrawHeaderPopup(Rect rect, int current, SerializedProperty headers)
    {
        int count = headers != null ? headers.arraySize : 0;
        var options = new string[count + 1];
        options[0] = NoHeader;
        for (int i = 0; i < count; i++)
        {
            string title = headers.GetArrayElementAtIndex(i).stringValue;
            options[i + 1] = $"{i}: {(string.IsNullOrEmpty(title) ? "(untitled)" : title)}";
        }

        // Popup index 0 = no header (-1); index n = header n - 1.
        int selected = current >= 0 && current < count ? current + 1 : 0;
        return EditorGUI.Popup(rect, selected, options) - 1;
    }
}
