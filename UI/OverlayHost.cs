using System;
using SCH1_StackLimitPlus.Game;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // The F6 window: a screen-space canvas holding a MenuView. Rebuilt after scene changes destroy it.
    public class OverlayHost
    {
        const float Width = 780, Height = 520;

        readonly Func<MenuView, bool> _addTabs;
        GameObject? _canvas;
        MenuView? _menu;
        RectTransform? _window;
        Vector2 _dragOffset;
        bool _dragging;

        public bool IsOpen => _canvas != null && _canvas.activeSelf;
        public MenuView? Menu => _menu;

        public OverlayHost(Func<MenuView, bool> addTabs) => _addTabs = addTabs;

        public void Toggle(string? tab = null)
        {
            if (IsOpen && tab == null) Close();
            else Open(tab);
        }

        public void Open(string? tab)
        {
            if (_canvas == null) Build();
            if (_canvas == null || _menu == null) return;
            if (!IsOpen)
            {
                _canvas.SetActive(true);
                InputBlocker.Block();
            }
            _menu.Show(tab ?? _menu.CurrentTitle ?? "Items");
        }

        public void Close()
        {
            if (!IsOpen) return;
            _canvas!.SetActive(false);
            _dragging = false;
            Il2CppScheduleOne.GameInput.IsTyping = false;
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
                _canvas = new GameObject("StackLimitPlusOverlay");
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

                _window = UiFactory.Node("Menu", _canvas.transform);
                _window.sizeDelta = new Vector2(Width, Height);
                _menu = new MenuView(_window, phone: false, onClose: Close);
                _addTabs(_menu);
                _canvas.SetActive(false);
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Building the menu failed: {ex}");
                if (_canvas != null) UnityEngine.Object.Destroy(_canvas);
                _canvas = null;
                _menu = null;
            }
        }

        // Called every frame from Core: Esc to close, drag by header.
        public void Tick()
        {
            if (!IsOpen || _window == null || _menu == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !Il2CppScheduleOne.GameInput.IsTyping) { Close(); return; }

            var canvas = _canvas!.GetComponent<Canvas>();
            float scale = canvas.scaleFactor <= 0 ? 1 : canvas.scaleFactor;
            Vector2 mouse = Input.mousePosition;
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(_menu.Header, mouse, null))
            {
                _dragging = true;
                _dragOffset = _window.anchoredPosition - mouse / scale;
            }
            if (!Input.GetMouseButton(0)) _dragging = false;
            if (_dragging)
            {
                var half = new Vector2(Screen.width, Screen.height) / scale / 2f;
                var pos = mouse / scale + _dragOffset;
                // keep at least the header on screen
                pos.x = Mathf.Clamp(pos.x, -half.x + 120, half.x - 120);
                pos.y = Mathf.Clamp(pos.y, -half.y + 40, half.y - Height / 2f + 10);
                _window.anchoredPosition = pos;
            }
        }
    }
}
