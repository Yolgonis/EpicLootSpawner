using System;
using EpicLootSpawner;

internal static class ResizeChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var screen in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (float scale in new[] { 0.75f, 1f, 1.5f })
        {
            var original = UiLayout.ForScreen(screen.Item1, screen.Item2, scale);
            var resized = UiLayout.ForScreen(screen.Item1, screen.Item2, scale, 1700, 1000);
            check(resized.Scale == original.Scale, "resizing never changes text scale");
            check(resized.Width * resized.Scale <= screen.Item1 - 23 && resized.Height * resized.Scale <= screen.Item2 - 23, "saved dimensions fit each tested screen and scale");
            check(Math.Abs(resized.LibraryWidth + resized.EditorWidth + resized.PreviewWidth - resized.Width + 64) < 0.001f
                && resized.PanelHeight == resized.Height - 150, "resized columns and scroll room follow window dimensions");
            var minimum = UiLayout.ForScreen(screen.Item1, screen.Item2, scale, 1, 1);
            check(minimum.Width == UiLayout.MinimumWidth && minimum.Height == UiLayout.MinimumHeight, "small dimensions retain readable three-column geometry");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f, 0f })
        {
            var fallback = UiLayout.ForScreen(1920, 1080, 1, invalid, invalid);
            check(fallback.Width == 1320 && fallback.Height == 900, "invalid or zero saved size uses legacy defaults");
        }
        var large = UiLayout.ForScreen(3840, 2160, 1, 2200, 1500);
        var reduced = UiLayout.ForScreen(1280, 720, 1, 2200, 1500);
        var restored = UiLayout.ForScreen(3840, 2160, 1, 2200, 1500);
        check(large.Width == 2200 && large.Height == 1500 && reduced.Width < large.Width && reduced.Height < large.Height
            && restored.Width == large.Width && restored.Height == large.Height, "screen clamp does not discard stored dimensions");

        var resize = new WindowResizeState();
        bool rejected = false;
        try { resize.Move(100, 100, 2000, 1500, out _, out _); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "drag requires an active resize session");
        foreach (float scale in new[] { 0.5f, 1f, 1.5f })
        {
            resize.Begin(400, 500, 1320, 900, scale);
            resize.Move(400 + 80 * scale, 500 + 60 * scale, 2500, 2000, out float width, out float height);
            check(width == 1400 && height == 960, "physical pointer delta converts through captured text scale");
            resize.Move(400, 500, 2500, 2000, out width, out height);
            check(width == 1320 && height == 900, "drag uses start anchor instead of accumulating prior moves");
            resize.Move(-99999, -99999, 2500, 2000, out width, out height);
            check(width == UiLayout.MinimumWidth && height == UiLayout.MinimumHeight, "drag beyond zero stops at the minimum instead of snapping to default");
            resize.Move(99999, 99999, 1600, 1000, out width, out height);
            check(width == 1600 && height == 1000, "growth respects right and bottom limits at current position");
            resize.Cancel(); check(!resize.Active, "cancel releases resize state");
        }
        foreach (float scale in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
        {
            rejected = false;
            try { resize.Begin(0, 0, 1320, 900, scale); } catch (ArgumentException) { rejected = true; }
            check(rejected && !resize.Active, "invalid captured scale cannot start resize");
        }
    }
}
