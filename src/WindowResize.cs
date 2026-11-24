using UnityEngine;

namespace EpicLootSpawner
{
    public sealed partial class Plugin
    {
        private readonly WindowResizeState windowResize = new WindowResizeState();
        private int resizeControl, resizeScreenWidth, resizeScreenHeight;
        private float resizeScale;
        private bool HasModal => picker != PickerKind.None || showPreview || showTransfer || showAppearance || showPresetManager
            || showCollection || showRecovery || showDisenchant || showInventoryUndo;

        private Rect ResizeGrip => new Rect(window.xMax - 34, window.yMax - 34, 28, 28);

        private void CancelWindowResize()
        {
            windowResize.Cancel();
            if (resizeControl != 0 && GUIUtility.hotControl == resizeControl) GUIUtility.hotControl = 0;
            resizeControl = 0;
        }

        private void HandleWindowResize()
        {
            int control = GUIUtility.GetControlID(190422, FocusType.Passive);
            var e = Event.current;
            var pointer = Input.mousePosition;
            float x = pointer.x, y = Screen.height - pointer.y;
            var grip = ResizeGrip;
            var physicalGrip = new Rect(grip.x * layout.Scale, grip.y * layout.Scale, grip.width * layout.Scale, grip.height * layout.Scale);
            if (windowResize.Active && (HasModal || GUIUtility.hotControl != resizeControl
                || Screen.width != resizeScreenWidth || Screen.height != resizeScreenHeight || layout.Scale != resizeScale)) CancelWindowResize();
            if (e.type == EventType.MouseDown && e.button == 0 && GUI.enabled && !HasModal
                && GUIUtility.hotControl == 0 && physicalGrip.Contains(new Vector2(x, y)))
            {
                windowResize.Begin(x, y, layout.Width, layout.Height, layout.Scale);
                resizeControl = control; resizeScale = layout.Scale;
                resizeScreenWidth = Screen.width; resizeScreenHeight = Screen.height;
                GUIUtility.hotControl = control; e.Use();
            }
            else if (windowResize.Active && GUIUtility.hotControl == resizeControl
                && (e.type == EventType.MouseDrag || (e.type == EventType.MouseUp && e.button == 0)))
            {
                windowResize.Move(x, y, Mathf.Min(layout.MaximumWidth, Screen.width / layout.Scale - window.x),
                    Mathf.Min(layout.MaximumHeight, Screen.height / layout.Scale - window.y), out float width, out float height);
                bool finished = e.type == EventType.MouseUp;
                // Apply between GUI events: changing layout during MouseDrag would
                // disagree with the last Layout pass and break GUILayout controls.
                QueueAction(() => ApplyWindowSize(width, height, finished));
                if (finished) CancelWindowResize();
                e.Use();
            }
        }

        private void DrawResizeGrip()
        {
            if (HasModal) return;
            GUI.Box(ResizeGrip, new GUIContent("◢", "Boyutlandırmak için sağ alt köşeyi sürükle"), section);
        }

        private void ApplyWindowSize(float width, float height, bool save)
        {
            preferredWidth = width; preferredHeight = height;
            UpdateAppearance();
            if (save) SaveAppearance();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) CancelWindowResize();
        }
    }
}
