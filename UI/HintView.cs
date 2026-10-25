using System;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // "[F6] Change limit" under the crosshair, styled like the game's own key prompts (dark key box, italic label).
    public class HintView
    {
        GameObject? _canvas;
        TextMeshProUGUI? _key;
        Image? _keyBox;

        public void Show(bool shown, string key)
        {
            if (!shown)
            {
                if (_canvas != null) _canvas.SetActive(false);
                return;
            }
            if (_canvas == null) Build();
            if (_canvas == null || _key == null || _keyBox == null) return;
            if (_key.text != key)
            {
                _key.text = key;
                UiFactory.Size(_keyBox, width: Mathf.Max(30, _key.preferredWidth + 18), height: 30);
            }
            _canvas.SetActive(true);
        }

        void Build()
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

                _keyBox = UiFactory.Panel(row.transform, new Color(0.12f, 0.12f, 0.12f, 0.8f), rounded: true, name: "Key");
                _keyBox.raycastTarget = false;
                _key = UiFactory.Text(_keyBox.transform, "", 15, Theme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiFactory.Stretch(_key.rectTransform);

                var label = UiFactory.Text(row.transform, "Change limit", 16, Theme.Text, FontStyles.Italic);
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
