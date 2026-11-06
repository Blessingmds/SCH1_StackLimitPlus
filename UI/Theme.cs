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

        // Neutral greys taken from the game's own HUD (hotbar slots, Phone/Character tabs): no hue, white for "active".
        public static readonly Color Scrim = Hex("#000000", 0.2f);
        public static readonly Color Window = Hex("#262626", 0.94f);
        public static readonly Color Sidebar = Hex("#262626", 0f);
        public static readonly Color Raised = Hex("#141414", 0.55f);
        public static readonly Color Hover = Hex("#3A3A3A", 0.9f);
        public static readonly Color Pressed = Hex("#474747", 0.9f);
        public static readonly Color Line = Hex("#FFFFFF", 0.12f);
        public static readonly Color Text = Hex("#F2F2F2");
        public static readonly Color TextMuted = Hex("#A8A8A8");
        public static readonly Color TextFaint = Hex("#767676");
        public static readonly Color Accent = Hex("#FFFFFF");
        public static readonly Color AccentText = Hex("#1A1A1A");
        public static readonly Color Danger = Hex("#E0574B");

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

        // 9-sliced rectangle with a 2px corner, drawn at runtime
        public static Sprite? Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                try
                {
                    const int size = 16, r = 2;
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

        static Sprite? _ring;

        // 1.5px circle outline, anti-aliased, for the ? help button
        public static Sprite? Ring
        {
            get
            {
                if (_ring != null) return _ring;
                try
                {
                    const int n = 64;
                    float r = n / 2f - 2f, c = n / 2f;
                    var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                            tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(2.5f - Mathf.Abs(d - r) * 1.2f)));
                        }
                    tex.Apply();
                    _ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    UnityEngine.Object.DontDestroyOnLoad(_ring);
                }
                catch (System.Exception ex) { Core.ErrorOnce($"Ring sprite unavailable: {ex.Message}"); }
                return _ring;
            }
        }

        public static void ResetCaches() => _font = null;
    }
}
