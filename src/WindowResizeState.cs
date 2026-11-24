using System;

namespace EpicLootSpawner
{
    internal sealed class WindowResizeState
    {
        public bool Active { get; private set; }
        private float pointerX, pointerY, width, height, scale;

        public void Begin(float x, float y, float startingWidth, float startingHeight, float startingScale)
        {
            if (startingScale <= 0 || float.IsNaN(startingScale) || float.IsInfinity(startingScale))
                throw new ArgumentException("Invalid resize scale.");
            pointerX = x; pointerY = y; width = startingWidth; height = startingHeight; scale = startingScale;
            Active = true;
        }

        public void Move(float x, float y, float maximumWidth, float maximumHeight, out float nextWidth, out float nextHeight)
        {
            if (!Active) throw new InvalidOperationException("Resize has not started.");
            nextWidth = UiLayout.Dimension(width + (x - pointerX) / scale, width, UiLayout.MinimumWidth, maximumWidth);
            nextHeight = UiLayout.Dimension(height + (y - pointerY) / scale, height, UiLayout.MinimumHeight, maximumHeight);
            // A drag past the minimum is a valid request for the minimum, not an
            // invalid saved preference (where zero means use the default).
            if (x < pointerX && width + (x - pointerX) / scale <= 0) nextWidth = UiLayout.MinimumWidth;
            if (y < pointerY && height + (y - pointerY) / scale <= 0) nextHeight = UiLayout.MinimumHeight;
        }

        public void Cancel() => Active = false;
    }
}
