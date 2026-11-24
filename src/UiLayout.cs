using System;

namespace EpicLootSpawner
{
    // Overlay opened from the design form instead of switching editor tabs.
    internal enum PickerKind { None, Effect, Stone, Special }

    internal sealed class UiLayout
    {
        public const float MinimumWidth = 1180, MinimumHeight = 740;
        public float Scale, Width, Height, PanelHeight, LibraryWidth, EditorWidth, PreviewWidth, MaximumWidth, MaximumHeight;
        public float ListNameWidth(float trailingWidth, bool insetCard = false)
            => Math.Max(48, LibraryWidth - 28 - 20 - 36 - trailingWidth - 12 - (insetCard ? 24 : 0));
        public static float NormalizeScale(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Math.Max(0.75f, Math.Min(1.5f, value));
        public static UiLayout ForScreen(int screenWidth, int screenHeight, float requested, float preferredWidth = 0, float preferredHeight = 0)
        {
            bool compact = screenWidth < 1600 || screenHeight < 900;
            float width = compact ? 1180 : 1320, height = compact ? 740 : 900;
            float scale = Math.Min(NormalizeScale(requested), Math.Min(Math.Max(1, screenWidth - 24) / width, Math.Max(1, screenHeight - 24) / height));
            // Resizing changes available space, not the text scale. Compute the
            // original screen-fit scale first, then clamp the chosen window size.
            float maximumWidth = Math.Max(MinimumWidth, Math.Max(1, screenWidth - 24) / scale);
            float maximumHeight = Math.Max(MinimumHeight, Math.Max(1, screenHeight - 24) / scale);
            width = Dimension(preferredWidth, width, MinimumWidth, maximumWidth);
            height = Dimension(preferredHeight, height, MinimumHeight, maximumHeight);
            float available = width - 64;
            // Top bar and the fixed action bar take 150 units; the three columns share the rest.
            return new UiLayout { Scale = scale, Width = width, Height = height, PanelHeight = height - 150,
                LibraryWidth = available * 0.24f, EditorWidth = available * 0.45f, PreviewWidth = available * 0.31f,
                MaximumWidth = maximumWidth, MaximumHeight = maximumHeight };
        }
        public static float Dimension(float value, float fallback, float minimum, float maximum)
            => float.IsNaN(value) || float.IsInfinity(value) || value <= 0 ? fallback
                : Math.Max(minimum, Math.Min(Math.Max(minimum, maximum), value));
        public static float Position(float value, float screenExtent, float windowExtent)
            => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(Math.Max(0, screenExtent - windowExtent), value));
    }
}
