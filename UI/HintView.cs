using System;
using InteractionCanvas = Il2CppScheduleOne.UI.InteractionCanvas;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // "[F6] Change limit" as a second line of the game's own interaction prompt ("[E] Use Packaging Station"):
    // a copy of the game's prompt, kept just under it. Falls back to a plain prompt under the crosshair.
    public class HintView
    {
        const float LineGap = 4;

        InteractionCanvas? _game;
        RectTransform? _copy;
        TextMeshProUGUI? _message, _key;
        bool _logged;

        // fallback
        GameObject? _canvas;
        TextMeshProUGUI? _fbKey;
        Image? _fbKeyBox;

        // Called from OnLateUpdate, after the game has placed its own prompt this frame.
        public void Show(bool shown, string key)
        {
            try
            {
                if (shown && (_copy == null || _game == null)) TryClone();
                if (_copy != null && _game != null)
                {
                    if (_canvas != null) _canvas.SetActive(false);
                    _copy.gameObject.SetActive(shown);
                    if (shown) Follow(key);
                    return;
                }
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Look hint could not use the game's prompt: {ex.Message}");
                _copy = null;
            }
            ShowFallback(shown, key);
        }

        void TryClone()
        {
            _game = UnityEngine.Object.FindObjectOfType<InteractionCanvas>();
            var container = _game?.Container;
            if (_game == null || container == null) return;
            var source = container.GetComponent<RectTransform>();
            var go = UnityEngine.Object.Instantiate(source.gameObject, source.parent);
            go.name = "StackLimitPlusPrompt";
            (go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>()).ignoreLayout = true;
            _copy = go.GetComponent<RectTransform>();
            if (!_logged) { _logged = true; MelonLogger.Msg("[hint] game prompt: " + Describe(source, 0)); }
            // the game's prompt script would keep redrawing the copy's icon (blank key box): ours is static
            foreach (var c in go.GetComponents<Component>())
                if (c.GetIl2CppType().Name == "InputPromptsItemUI") UnityEngine.Object.DestroyImmediate(c);

            // the copy's parts sit at the same paths as the game's (key box: Backdrop + Image + SpriteLabel)
            _message = Twin(source, _copy, _game.MessageText?.transform);
            _key = Twin(source, _copy, _game.IconText?.transform);
            _keyParts = Parts(_copy);
            _gameKeyParts = Parts(source);
            if (_message != null) _message.color = _game.DefaultMessageColor;
            if (_message == null || _key == null) Core.ErrorOnce("Look hint: game prompt layout changed; text may be missing.");
        }

        Transform?[] _keyParts = Array.Empty<Transform?>(), _gameKeyParts = Array.Empty<Transform?>();
        static readonly string[] KeyPartPaths = { "PromptImage1/Backdrop", "PromptImage1/Image", "PromptImage1/SpriteLabel" };

        static Transform?[] Parts(Transform root) => Array.ConvertAll(KeyPartPaths, p => (Transform?)root.Find(p));

        // copy how the game draws a keyboard key right now ("E"), so "F6" matches it; keep the last good look otherwise
        void MirrorKeyBox()
        {
            var gameLabel = _gameKeyParts.Length > 2 ? _gameKeyParts[2] : null;
            bool gameShowsKey = gameLabel != null && gameLabel.gameObject.activeInHierarchy;
            for (int i = 0; i < _keyParts.Length; i++)
            {
                var mine = _keyParts[i];
                var theirs = _gameKeyParts[i];
                if (mine == null) continue;
                if (gameShowsKey && theirs != null)
                {
                    mine.gameObject.SetActive(theirs.gameObject.activeSelf);
                    var a = theirs.GetComponent<Image>();
                    var b = mine.GetComponent<Image>();
                    if (a != null && b != null) { b.sprite = a.sprite; b.color = a.color; b.type = a.type; b.pixelsPerUnitMultiplier = a.pixelsPerUnitMultiplier; }
                    mine.GetComponent<RectTransform>().sizeDelta = theirs.GetComponent<RectTransform>().sizeDelta;
                    _mirrored = true;
                }
                else if (!_mirrored) mine.gameObject.SetActive(i != 1); // before we've seen a key: backdrop + letter, no picture
            }
        }

        bool _mirrored;

        TextMeshProUGUI? Twin(Transform root, Transform copy, Transform? original) => Twin<TextMeshProUGUI>(root, copy, original);

        static T? Twin<T>(Transform root, Transform copy, Transform? original) where T : Component
        {
            if (original == null) return null;
            if (original == root) return copy.GetComponent<T>();
            var path = original.name;
            for (var t = original.parent; t != null && t != root; t = t.parent) path = t.name + "/" + path;
            return copy.Find(path)?.GetComponent<T>();
        }

        void Follow(string key)
        {
            var source = _game!.Container.GetComponent<RectTransform>();
            if (_message != null && _message.text != "Change limit") _message.text = "Change limit";
            if (_key != null && _key.text != key) _key.text = key;
            MirrorKeyBox();
            _copy!.localScale = source.localScale;
            // one line below the game's prompt (or where it would be when the game shows none)
            _copy.anchorMin = source.anchorMin; _copy.anchorMax = source.anchorMax; _copy.pivot = source.pivot;
            float height = source.rect.height > 0 ? source.rect.height : 40;
            _copy.anchoredPosition = source.anchoredPosition - new Vector2(0, (height + LineGap) * source.localScale.y);
            _copy.SetAsLastSibling();
        }

        static string Describe(Transform t, int depth)
        {
            var parts = t.name + "[" + string.Join(",", System.Linq.Enumerable.Select(t.GetComponents<Component>(), c => c.GetIl2CppType().Name)) + "]";
            if (depth > 3 || t.childCount == 0) return parts;
            var children = new string[t.childCount];
            for (int i = 0; i < t.childCount; i++) children[i] = Describe(t.GetChild(i), depth + 1);
            return parts + "{" + string.Join(" ", children) + "}";
        }

        void ShowFallback(bool shown, string key)
        {
            if (!shown)
            {
                if (_canvas != null) _canvas.SetActive(false);
                return;
            }
            if (_canvas == null) BuildFallback();
            if (_canvas == null || _fbKey == null || _fbKeyBox == null) return;
            if (_fbKey.text != key)
            {
                _fbKey.text = key;
                UiFactory.Size(_fbKeyBox, width: Mathf.Max(30, _fbKey.preferredWidth + 18), height: 30);
            }
            _canvas.SetActive(true);
        }

        void BuildFallback()
        {
            try
            {
                _canvas = new GameObject("StackLimitPlusHint");
                var canvas = _canvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400;
                var scaler = _canvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;

                var row = UiFactory.Row(_canvas.transform, 10, 30, "Prompt");
                row.childAlignment = TextAnchor.MiddleCenter;
                var rt = row.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(400, 30);
                rt.anchoredPosition = new Vector2(0, -90);

                _fbKeyBox = UiFactory.Panel(row.transform, new Color(0.12f, 0.12f, 0.12f, 0.8f), rounded: true, name: "Key");
                _fbKeyBox.raycastTarget = false;
                _fbKey = UiFactory.Text(_fbKeyBox.transform, "", 15, Theme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiFactory.Stretch(_fbKey.rectTransform);

                var label = UiFactory.Text(row.transform, "Change limit", 16, Theme.Text);
                UiFactory.Size(label, width: label.preferredWidth + 4, height: 30);
                _canvas.SetActive(false);
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Building the look hint failed: {ex.Message}");
                if (_canvas != null) UnityEngine.Object.Destroy(_canvas);
                _canvas = null;
            }
        }
    }
}
