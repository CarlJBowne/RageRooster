// PSEUDOCODE / PLAN (detailed):
// - Create an EditorWindow named ColorStripImageGenerator under the Tools menu.
// - Keep a serializable List<ColorEntry> where ColorEntry holds a Color and a Name.
// - OnEnable:
//     - Ensure the list is initialized and, if empty, call ResetToDefaults.
// - ResetToDefaults:
//     - Use reflection to find all public static properties on UnityEngine.Color that return Color.
//     - For each property, read its Color value and Name and add a ColorEntry.
//     - If reflection yields no entries, add a small fallback list of basic colors.
// - OnGUI:
//     - Show a header indicating this is a display-only color strip.
//     - Provide buttons: Add Color, Remove Last, Reset Defaults.
//     - For each ColorEntry show:
//         - Editable ColorField for the color.
//         - Editable TextField for the Name.
//         - Move up / move down / remove buttons.
//     - Do NOT include any UI or methods that save/export images or write files.
// - Keep editor-only with #if UNITY_EDITOR and use UnityEditor APIs.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

public class ColorStripImageGenerator : EditorWindow
{
    [System.Serializable]
    private class ColorEntry
    {
        public Color Color;
        public string Name;

        public ColorEntry() { Color = Color.white; Name = string.Empty; }
        public ColorEntry(Color c, string n) { Color = c; Name = n ?? string.Empty; }
    }

    [SerializeField]
    private List<ColorEntry> colors = new List<ColorEntry>();
    private Vector2 scroll;

    [MenuItem("Tools/Color Strip Image Generator")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<ColorStripImageGenerator>("Color Strip Image");
        wnd.minSize = new Vector2(360, 240);
    }

    private void OnEnable()
    {
        if (colors == null)
            colors = new List<ColorEntry>();

        if (colors.Count == 0)
            ResetToDefaults();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Color Strip (Display Only)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Color")) colors.Add(new ColorEntry(Color.white, string.Empty));
        if (GUILayout.Button("Remove Last")) { if (colors.Count > 0) colors.RemoveAt(colors.Count - 1); }
        if (GUILayout.Button("Reset Defaults")) ResetToDefaults();
        if (GUILayout.Button("Sort by Hue")) { SortByHue(); Repaint(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Colors ({colors.Count})", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < colors.Count; i++)
        {
            var entry = colors[i];
            EditorGUILayout.BeginHorizontal();

            // Color field
            entry.Color = EditorGUILayout.ColorField(entry.Color, GUILayout.Width(160));

            // Name field
            entry.Name = EditorGUILayout.TextField(entry.Name, GUILayout.Width(200));

            // Move up
            if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0)
            {
                var tmp = colors[i - 1];
                colors[i - 1] = colors[i];
                colors[i] = tmp;
            }

            // Move down
            if (GUILayout.Button("▼", GUILayout.Width(24)) && i < colors.Count - 1)
            {
                var tmp = colors[i + 1];
                colors[i + 1] = colors[i];
                colors[i] = tmp;
            }

            // Remove
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                colors.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void SortByHue()
    {
        if (colors == null || colors.Count <= 1)
            return;

        colors.Sort((a, b) =>
        {
            Color.RGBToHSV(a.Color, out var ha, out var sa, out var va);
            Color.RGBToHSV(b.Color, out var hb, out var sb, out var vb);

            int cmp = ha.CompareTo(hb);
            if (cmp != 0) return cmp;

            // Tie-break: by saturation then value then name to make sort deterministic
            cmp = sa.CompareTo(sb);
            if (cmp != 0) return cmp;

            cmp = va.CompareTo(vb);
            if (cmp != 0) return cmp;

            return string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
        });
    }

    private void ResetToDefaults()
    {
        colors.Clear();

        var props = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Color));

        foreach (var prop in props.OrderBy(p => p.Name))
        {
            try
            {
                var value = (Color)prop.GetValue(null);
                colors.Add(new ColorEntry(value, prop.Name));
            }
            catch
            {
                // Ignore any property we cannot read
            }
        }

        if (colors.Count == 0)
        {
            colors.Add(new ColorEntry(Color.red, "red"));
            colors.Add(new ColorEntry(Color.green, "green"));
            colors.Add(new ColorEntry(Color.blue, "blue"));
            colors.Add(new ColorEntry(Color.white, "white"));
            colors.Add(new ColorEntry(Color.black, "black"));
        }
    }
}
#endif