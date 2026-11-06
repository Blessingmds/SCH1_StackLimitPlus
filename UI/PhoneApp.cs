using System;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // A "StackLimit" icon on the phone home screen that shows the menu inside the phone screen.
    public class PhoneApp
    {
        readonly Func<MenuView, bool> _addTabs;
        GameObject? _icon;
        GameObject? _root;
        MenuView? _menu;
        HomeScreen? _home;
        float _retry;
        bool _loggedHierarchy;
        bool _fallback;

        Collider? _homeButton;
        Camera? _phoneCamera;
        Canvas? _canvas;
        GraphicRaycaster? _raycaster;
        public bool IsOpen => _root != null && _canvas != null && _canvas.enabled;

        // Hide by switching our own nested canvas off, never the GameObject: re-activating hundreds of
        // item rows makes Unity rebuild all their layout and text, a 300-600ms hitch on every open/close.
        void SetVisible(bool on)
        {
            _canvas!.enabled = on;
            _raycaster!.enabled = on && !_locked;
        }

        // the tutorial shows the app without letting the player change anything in it
        bool _locked;
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                if (_raycaster != null && _canvas != null) _raycaster.enabled = _canvas.enabled && !value;
            }
        }

        // closes the app and puts the phone away (the tutorial's phone step)
        public void CloseWithPhone()
        {
            Close();
            // the game's own "put the phone away" (Phone.SetIsOpen alone left it raised behind the menu)
            try { UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.UI.GameplayMenu>()?.Close(); }
            catch (Exception ex) { Core.ErrorOnce($"Closing the phone failed: {ex.Message}"); }
        }

        public PhoneApp(Func<MenuView, bool> addTabs) => _addTabs = addTabs;

        public void Tick()
        {
            try
            {
                if (_icon == null && (_retry -= Time.unscaledDeltaTime) <= 0)
                {
                    _retry = 2f;
                    CreateIcon();
                }
                if (!IsOpen) return;
                var phone = PlayerSingleton<Phone>.Instance;
                if (phone == null || !phone.IsOpen) Close();
                else if (Input.GetMouseButtonDown(0) && _homeButton != null && _phoneCamera != null
                    && _homeButton.Raycast(_phoneCamera.ScreenPointToRay(Input.mousePosition), out _, 100f)) Close();
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Phone app failed: {ex.Message}");
            }
        }

        public void Refresh()
        {
            if (IsOpen) _menu?.Refresh();
        }

        void CreateIcon()
        {
            _home = UnityEngine.Object.FindObjectOfType<HomeScreen>();
            if (_home == null || _home.appIconPrefab == null || _home.appIconContainer == null) return;

            _icon = UnityEngine.Object.Instantiate(_home.appIconPrefab, _home.appIconContainer);
            _icon.name = "StackLimitPlusIcon";
            if (!_loggedHierarchy)
            {
                _loggedHierarchy = true;
                MelonLogger.Msg("[phone] app icon template: " + Describe(_icon.transform, 0));
            }

            // template: Shade, Outline, Mask/Image (the picture), Notifications/Text (badge), Label
            var label = _icon.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.text = "StackLimit";
            var picture = _icon.transform.Find("Mask/Image")?.GetComponent<Image>();
            var sprite = IconSprite();
            if (picture != null && sprite != null) picture.sprite = sprite;
            _icon.transform.Find("Notifications")?.gameObject.SetActive(false);
            if (label == null || picture == null) Core.ErrorOnce("Phone app icon template changed; icon may look wrong.");
            _icon.transform.SetAsLastSibling();

            var button = _icon.GetComponent<Button>() ?? _icon.GetComponentInChildren<Button>();
            if (button != null) UiFactory.Listen(button, Open);
            else Core.ErrorOnce("Phone app icon has no button.");
        }

        static string Describe(Transform t, int depth)
        {
            var parts = t.name + "[" + string.Join(",", System.Linq.Enumerable.Select(t.GetComponents<Component>(), c => c.GetIl2CppType().Name)) + "]";
            if (depth > 3 || t.childCount == 0) return parts;
            var children = new string[t.childCount];
            for (int i = 0; i < t.childCount; i++) children[i] = Describe(t.GetChild(i), depth + 1);
            return parts + "{" + string.Join(" ", children) + "}";
        }

        void Open()
        {
            if (_fallback) { Core.Overlay.Open(null); return; }
            try
            {
                if (_root == null) Build();
                if (_root == null || _menu == null) { _fallback = true; Core.Overlay.Open(null); return; }
                var phone = PlayerSingleton<Phone>.Instance;
                phone?.SetIsHorizontal(true);
                phone?.SetLookOffsetMultiplier(0.6f); // same close-up view as the game's Map app
                _home?.SetIsOpen(false);
                SetVisible(true);
                _root.transform.SetAsLastSibling();
                RevealParents();
                Core.Catalog.Rescan();
                _menu.Show(_menu.CurrentTitle ?? "Items");
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Opening the phone app failed, using the F6 window instead: {ex.Message}");
                _fallback = true;
                Core.Overlay.Open(null);
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            SetVisible(false);
            RestoreParents();
            GameInput.IsTyping = false;
            try
            {
                var phone = PlayerSingleton<Phone>.Instance;
                phone?.SetIsHorizontal(false);
                phone?.SetLookOffsetMultiplier(1f);
                _home?.SetIsOpen(true);
            }
            catch (Exception ex) { Core.ErrorOnce($"Closing the phone app failed: {ex.Message}"); }
        }

        void OnExit(ExitAction action)
        {
            if (action.Used || !IsOpen) return;
            action.Used = true;
            Close();
        }

        void Build()
        {
            // Mirror the Deliveries app: a sibling of its object, with its rect, holding a copy of its screen container's rect.
            // (Parenting inside DeliveryApp itself left us hidden whenever that app was closed.)
            DeliveryApp? reference = null;
            foreach (var app in Resources.FindObjectsOfTypeAll<DeliveryApp>())
                if (app.gameObject.scene.IsValid()) { reference = app; break; }
            var appRect = reference?.GetComponent<RectTransform>();
            var container = reference?.appContainer;
            if (appRect == null || container == null || appRect.parent == null) { Core.ErrorOnce("Phone app area not found."); return; }

            var shell = UiFactory.Node("StackLimitPlusApp", appRect.parent);
            CopyRect(appRect, shell);
            _canvas = shell.gameObject.AddComponent<Canvas>(); // nested: inherits the phone's sorting, hides cheaply
            _raycaster = shell.gameObject.AddComponent<GraphicRaycaster>();
            _canvas.enabled = _raycaster.enabled = false;
            // opaque base: the phone's own apps cover the home screen completely
            var root = UiFactory.Panel(shell, new Color(0.15f, 0.15f, 0.15f, 1f), name: "Screen").rectTransform;
            CopyRect(container, root);
            _root = shell.gameObject;
            MelonLogger.Msg($"[phone] app placed under {appRect.parent.name}, screen {container.rect.size}, layer {shell.gameObject.layer}");
            // lay out at half size and scale up 2x: the phone screen is ~1200 units wide but small on screen
            const float scale = 2f;
            var scaled = UiFactory.Node("Scaled", root);
            scaled.anchorMin = scaled.anchorMax = scaled.pivot = new Vector2(0.5f, 0.5f);
            scaled.sizeDelta = container.rect.size / scale;
            scaled.localScale = Vector3.one * scale;
            _menu = new MenuView(scaled, phone: true, onClose: Close);
            _addTabs(_menu);

            // close like the game's own apps: on the phone model's home button, the close-all-apps broadcast and the exit input
            var phone = PlayerSingleton<Phone>.Instance;
            if (phone != null)
            {
                phone.closeApps += (Il2CppSystem.Action)(Action)Close;
                // the home button is a 3D collider on the phone model (Phone/phone/Button), clicked via raycast, not a UI Button
                foreach (var c in phone.GetComponentsInChildren<Collider>(true))
                    if (c.name == "Button") { _homeButton = c; break; }
                _phoneCamera = phone.GetComponentInParent<Camera>();
                if (_homeButton == null || _phoneCamera == null)
                    Core.ErrorOnce("Phone home button not found; use the X to close the StackLimit app.");
            }
            GameInput.RegisterExitListener(DelegateSupport.ConvertDelegate<GameInput.ExitDelegate>(new Action<ExitAction>(OnExit)), 10);
        }

        // The game shows its apps canvas only while one of its own apps is open; ours isn't one of them,
        // so switch on whatever hides it and put it back on close.
        readonly System.Collections.Generic.List<Action> _restore = new();
        bool _loggedParents;

        void RevealParents()
        {
            RestoreParents();
            var log = new System.Text.StringBuilder("[phone] parents:");
            for (var t = _root!.transform.parent; t != null; t = t.parent)
            {
                var go = t.gameObject;
                var canvas = go.GetComponent<Canvas>();
                var group = go.GetComponent<CanvasGroup>();
                log.Append($" {go.name}(active={go.activeSelf}, canvas={canvas?.enabled.ToString() ?? "-"}, alpha={group?.alpha.ToString() ?? "-"})");
                if (!go.activeSelf) { go.SetActive(true); _restore.Add(() => go.SetActive(false)); }
                if (canvas != null && !canvas.enabled) { canvas.enabled = true; _restore.Add(() => canvas.enabled = false); }
                if (group != null && group.alpha < 1f)
                {
                    float alpha = group.alpha;
                    group.alpha = 1f;
                    _restore.Add(() => group.alpha = alpha);
                }
                if (t.GetComponent<Phone>() != null) break;
            }
            if (!_loggedParents) { _loggedParents = true; MelonLogger.Msg(log.ToString()); }
        }

        void RestoreParents()
        {
            foreach (var undo in _restore)
            {
                try { undo(); } catch (Exception) { } // object may be gone after a scene change
            }
            _restore.Clear();
        }

        static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition; to.sizeDelta = from.sizeDelta;
            to.localRotation = from.localRotation; to.localScale = from.localScale;
        }

        // 128px rounded square with three stacked crates, in the phone's flat icon style
        static Sprite? _sprite;
        static Sprite? IconSprite()
        {
            if (_sprite != null) return _sprite;
            try
            {
                const int n = 128, r = 26;
                var bg = new Color(0.36f, 0.42f, 0.45f);
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (n - r)));
                        float dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (n - r)));
                        float a = (dx > 0 && dy > 0) ? Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f) : 1f;
                        var c = InCrate(x, y) ? Color.white : bg;
                        c.a = a;
                        tex.SetPixel(x, y, c);
                    }
                tex.Apply();
                _sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                UnityEngine.Object.DontDestroyOnLoad(tex);
                UnityEngine.Object.DontDestroyOnLoad(_sprite);
            }
            catch (Exception ex) { Core.ErrorOnce($"Phone icon could not be drawn: {ex.Message}"); }
            return _sprite;
        }

        // two crates side by side with one on top; each has a lid line cut out
        static bool InCrate(int x, int y) =>
            Crate(x, y, 26, 24) || Crate(x, y, 66, 24) || Crate(x, y, 46, 64);

        static bool Crate(int x, int y, int left, int bottom)
        {
            const int w = 36, h = 36;
            if (x < left || x >= left + w || y < bottom || y >= bottom + h) return false;
            bool lidGap = y >= bottom + h - 12 && y < bottom + h - 8 && x > left + 3 && x < left + w - 4;
            return !lidGap;
        }
    }
}
