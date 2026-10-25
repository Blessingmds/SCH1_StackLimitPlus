using System.Linq;
using Il2CppTMPro;
using UnityEngine;

namespace SCH1_StackLimitPlus.UI
{
    public static class Theme
    {
        static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = a;
            return c;
        }

        public static readonly Color Scrim = Hex("#000000", 0.35f);
        public static readonly Color Window = Hex("#141618", 0.98f);
        public static readonly Color Sidebar = Hex("#0F1113", 1f);
        public static readonly Color Raised = Hex("#1E2125");
        public static readonly Color Hover = Hex("#272B30");
        public static readonly Color Pressed = Hex("#30353B");
        public static readonly Color Line = Hex("#2A2E33");
        public static readonly Color Text = Hex("#EDEFF1");
        public static readonly Color TextMuted = Hex("#8E959D");
        public static readonly Color TextFaint = Hex("#5D646C");
        public static readonly Color Accent = Hex("#6CCB7A");
        public static readonly Color AccentText = Hex("#0E1A10");
        public static readonly Color Danger = Hex("#E5675B");

        public const float Title = 20f;
        public const float Body = 15f;
        public const float Small = 12.5f;
        public const float RowHeight = 44f;
        public const float Pad = 20f;

        static TMP_FontAsset? _font;
        static Sprite? _rounded;

        // the font most of the game's own UI text uses
        public static TMP_FontAsset? Font
        {
            get
            {
                if (_font != null) return _font;
                var texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                _font = texts.Where(t => t.font != null).GroupBy(t => t.font.name)
                    .OrderByDescending(g => g.Count()).Select(g => g.First().font).FirstOrDefault();
                return _font;
            }
        }

        // 9-sliced rounded rectangle, radius 4px, drawn at runtime
        public static Sprite? Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                try
                {
                    const int size = 16, r = 4;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r)));
                            float dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r)));
                            float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                            if (dx == 0 || dy == 0) alpha = 1f;
                            tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                        }
                    tex.Apply();
                    _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 1, r + 1, r + 1, r + 1));
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    UnityEngine.Object.DontDestroyOnLoad(_rounded);
                }
                catch (System.Exception ex)
                {
                    Core.ErrorOnce($"Rounded sprite unavailable, using square corners: {ex.Message}");
                }
                return _rounded;
            }
        }

        public static void ResetCaches() => _font = null;
    }
}
