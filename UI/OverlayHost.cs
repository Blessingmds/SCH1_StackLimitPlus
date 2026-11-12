using System;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // The F6 window: a screen-space canvas holding a MenuView. Rebuilt after scene changes destroy it.
    // Drag by the header, resize from the bottom-right grip; the size is remembered.
    public class OverlayHost
    {
        const float DefaultWidth = 780, DefaultHeight = 520, GripSize = 20;

        readonly Func<MenuView, bool> _addTabs;
        GameObject? _canvas;
        MenuView? _menu;
        Tutorial? _tutorial;
        RectTransform? _window;
        RectTransform? _grip;
        Vector2 _dragOffset;
        Vector2 _resizeStartMouse, _resizeStartSize;
        bool _dragging, _resizing;

        public bool IsOpen => _canvas != null && _canvas.GetComponent<Canvas>().enabled;

        // toggle the Canvas, not the GameObject: re-activating the item rows forces a full layout/text rebuild (hitch)
        void SetVisible(bool on)
        {
            _canvas!.GetComponent<Canvas>().enabled = on;
            _canvas.GetComponent<GraphicRaycaster>().enabled = on;
        }
        public MenuView? Menu => _menu;

        public OverlayHost(Func<MenuView, bool> addTabs) => _addTabs = addTabs;

        public void Open(string? tab)
        {
            if (_canvas == null) Build();
            if (_canvas == null || _menu == null) return;
            if (!IsOpen)
            {
                SetVisible(true);
                InputBlocker.Block();
            }
            _menu.Show(tab ?? _menu.CurrentTitle ?? "Items");
            if (_tutorial == null) return;
            if (_tutorial.WaitingForQuest) _tutorial.ResumeAfterQuest();
            else if (!Core.Settings.HasTutorialCompleted && !_tutorial.Running) _tutorial.Start(null);
        }

        // scene change or quit: undo the tour's test-run changes before the UI goes away
        public void EndTutorial() => _tutorial?.Finish();

        // page null = full tour; called by the ? button (current page) and Settings
        public void StartTutorial(string? page)
        {
            Open(null);
            if (_tutorial != null && !_tutorial.Running) _tutorial.Start(page);
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (_tutorial != null && !_tutorial.WaitingForQuest) _tutorial.Finish(); // closing mid-tour counts as skipping it
            SetVisible(false);
            _dragging = _resizing = false;
            MDSAPI.Players.Controls.IsTyping = false;
            InputBlocker.Unblock();
        }

        public void Refresh()
        {
            if (IsOpen) _menu?.Refresh();
        }

        void Build()
        {
            try
            {
                _canvas = new GameObject("StackLimitPlusOverlay") { layer = 5 };
                var canvas = _canvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                var scaler = _canvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;
                _canvas.AddComponent<GraphicRaycaster>();
                if (UnityEngine.EventSystems.EventSystem.current == null)
                    Core.ErrorOnce("No EventSystem in the scene: the menu can't receive clicks.");

                var scrim = UiFactory.Panel(_canvas.transform, Theme.Scrim, name: "Scrim");
                UiFactory.Stretch(scrim.rectTransform);

                // pivot top-left so resizing grows to the right and down, like a desktop window
                _window = UiFactory.Node("Menu", _canvas.transform);
                _window.pivot = new Vector2(0, 1);
                var size = new Vector2(Core.Settings.WindowWidth ?? DefaultWidth, Core.Settings.WindowHeight ?? DefaultHeight);
                _window.sizeDelta = size;
                _window.anchoredPosition = new Vector2(-size.x / 2f, size.y / 2f);
                _menu = new MenuView(_window, phone: false, onClose: Close, onHelp: StartTutorial);
                _addTabs(_menu);
                _grip = BuildGrip(_window);
                _tutorial?.Finish(); // a tour from before a scene change: undo its changes before replacing it
                _tutorial = new Tutorial(_canvas.transform, _menu, Close);
                SetVisible(false);
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Building the menu failed: {ex}");
                if (_canvas != null) UnityEngine.Object.Destroy(_canvas);
                _canvas = null;
                _menu = null;
            }
        }

        // three short diagonal strokes in the corner
        static RectTransform BuildGrip(RectTransform window)
        {
            var grip = UiFactory.Node("ResizeGrip", window);
            grip.anchorMin = grip.anchorMax = grip.pivot = new Vector2(1, 0);
            grip.sizeDelta = new Vector2(GripSize, GripSize);
            grip.anchoredPosition = new Vector2(-2, 2);
            for (int i = 0; i < 3; i++)
            {
                var bar = UiFactory.Panel(grip, Theme.TextFaint, name: "Stroke");
                bar.raycastTarget = false;
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
                rt.sizeDelta = new Vector2(4 + i * 5, 1.5f);
                rt.anchoredPosition = new Vector2(-4 - i * 2.2f, 4 + i * 2.2f);
                rt.localRotation = Quaternion.Euler(0, 0, 45);
            }
            return grip;
        }

        // Called every frame from Core: Esc to close, drag by header, resize by grip.
        public void Tick()
        {
            _tutorial?.TickQuest(); // the phone step runs while this window is closed
            if (!IsOpen || _window == null || _menu == null) return;
            if (_tutorial != null && _tutorial.Running && !_tutorial.WaitingForQuest) { _tutorial.Tick(); return; } // controls locked: no Esc-close, drag or resize
            if (Input.GetKeyDown(KeyCode.Escape) && !MDSAPI.Players.Controls.IsTyping) { Close(); return; }

            var canvas = _canvas!.GetComponent<Canvas>();
            float scale = canvas.scaleFactor <= 0 ? 1 : canvas.scaleFactor;
            Vector2 mouse = Input.mousePosition;
            var screen = new Vector2(Screen.width, Screen.height) / scale;

            if (Input.GetMouseButtonDown(0))
            {
                if (_grip != null && RectTransformUtility.RectangleContainsScreenPoint(_grip, mouse, null))
                {
                    _resizing = true;
                    _resizeStartMouse = mouse / scale;
                    _resizeStartSize = _window.sizeDelta;
                }
                else if (RectTransformUtility.RectangleContainsScreenPoint(_menu.Header, mouse, null))
                {
                    _dragging = true;
                    _dragOffset = _window.anchoredPosition - mouse / scale;
                }
            }
            if (!Input.GetMouseButton(0))
            {
                if (_resizing) SaveSize();
                _dragging = _resizing = false;
            }

            var half = screen / 2f;
            if (_resizing)
            {
                var delta = mouse / scale - _resizeStartMouse;
                var top = _window.anchoredPosition;
                float maxW = half.x - top.x, maxH = top.y + half.y;
                _window.sizeDelta = new Vector2(
                    Mathf.Clamp(_resizeStartSize.x + delta.x, SettingsStore.MinWindowWidth, Mathf.Max(SettingsStore.MinWindowWidth, maxW)),
                    Mathf.Clamp(_resizeStartSize.y - delta.y, SettingsStore.MinWindowHeight, Mathf.Max(SettingsStore.MinWindowHeight, maxH)));
            }
            else if (_dragging)
            {
                var pos = mouse / scale + _dragOffset;
                // keep the header reachable
                pos.x = Mathf.Clamp(pos.x, -half.x - _window.sizeDelta.x + 120, half.x - 120);
                pos.y = Mathf.Clamp(pos.y, -half.y + 50, half.y);
                _window.anchoredPosition = pos;
            }
        }

        void SaveSize()
        {
            if (_window == null) return;
            Core.Settings.WindowWidth = Mathf.RoundToInt(_window.sizeDelta.x);
            Core.Settings.WindowHeight = Mathf.RoundToInt(_window.sizeDelta.y);
            Core.SaveLocal();
        }
    }
}
