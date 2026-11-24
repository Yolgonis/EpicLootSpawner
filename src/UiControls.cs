using System.Collections.Generic;
using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<float, Dictionary<string, float>> nameHeights = new Dictionary<float, Dictionary<string, float>>();

        private bool NameButton(string title, string tooltip, bool current, float trailingWidth, bool insetCard = false, float? widthOverride = null)
        {
            float width = widthOverride.HasValue ? Mathf.Max(48, widthOverride.Value) : layout.ListNameWidth(trailingWidth, insetCard);
            if (!nameHeights.TryGetValue(width, out var heights))
                nameHeights[width] = heights = new Dictionary<string, float>();
            var content = new GUIContent(title, tooltip);
            if (!heights.TryGetValue(title, out float height))
                heights[title] = height = Mathf.Max(44, theme.button.CalcHeight(content, width));
            return GUILayout.Button(content, current ? selected : theme.button, GUILayout.Width(width), GUILayout.Height(height));
        }

        private void DrawTooltip()
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(GUI.tooltip)) return;
            string text = GUI.tooltip;
            float width = Mathf.Min(620, window.width - 36);
            float height = Mathf.Min(180, tooltipStyle.CalcHeight(new GUIContent(text), width));
            // Painted after the windows, inside the same scaled coordinate system.
            // Repaint only: the hint cannot steal input from a button beneath it.
            GUI.Box(new Rect(window.x + 18, window.y + window.height - height - 14, width, height), text, tooltipStyle);
        }

        private void DrawItemHeading(string text, float width)
        {
            width = Mathf.Max(48, width);
            var content = new GUIContent(text, text);
            float height = Mathf.Clamp(section.CalcHeight(content, width), 24, 64);
            GUILayout.Label(content, section, GUILayout.Width(width), GUILayout.Height(height));
        }

        private void WrappedHint(string text, GUIStyle style, float width, float minimum)
        {
            var content = new GUIContent(text ?? "", text ?? "");
            float height = Mathf.Max(minimum, style.CalcHeight(content, Mathf.Max(48, width)));
            GUILayout.Label(content, style, GUILayout.Height(height));
        }

        private string SearchInput(string value, string hint)
        {
            GUILayout.BeginHorizontal();
            string next = GUILayout.TextField(value, GUILayout.Height(34));
            // IMGUI has no placeholder text: paint the hint over the empty field.
            if (string.IsNullOrEmpty(value) && Event.current.type == EventType.Repaint)
                GUI.Label(GUILayoutUtility.GetLastRect(), hint, placeholder);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !string.IsNullOrEmpty(value);
            if (GUILayout.Button(new GUIContent("×", "Aramayı temizle"), GUILayout.Width(36), GUILayout.Height(34))) next = "";
            GUI.enabled = enabled;
            GUILayout.EndHorizontal();
            return next;
        }
    }
}
