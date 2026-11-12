using System;
using MDSAPI.Players;
using MDSAPI.Unity;
#if MONO
using TMPro;
#else
using Il2CppTMPro;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // uGUI building blocks. IMGUI is not used: several of its methods are stripped in this game build.
    public static class UiFactory
    {
        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name);
            // same layer as the parent: the phone screen is drawn by a camera that only renders its own layer
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static Image Panel(Transform parent, Color color, bool rounded = false, string name = "Panel")
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded && Theme.Rounded != null)
            {
                img.sprite = Theme.Rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexWidth = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
            if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
            if (flexWidth >= 0) le.flexibleWidth = flexWidth;
            return le;
        }

        public static HorizontalLayoutGroup Row(Transform parent, float spacing = 12, float height = Theme.RowHeight, string name = "Row")
        {
            var rt = Node(name, parent);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            Size(h, height: height);
            return h;
        }

        public static VerticalLayoutGroup Column(Transform parent, float spacing = 0, string name = "Column")
        {
            var rt = Node(name, parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size = Theme.Body, Color? color = null,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, string name = "Text")
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Theme.Font != null) t.font = Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? Theme.Text;
            t.fontStyle = style;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        static ColorBlock Colors(Color normal, Color hover, Color pressed)
        {
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = normal; cb.highlightedColor = hover; cb.pressedColor = pressed;
            cb.selectedColor = normal; cb.disabledColor = new Color(normal.r, normal.g, normal.b, normal.a * 0.4f);
            cb.colorMultiplier = 1f; cb.fadeDuration = 0.08f;
            return cb;
        }

        public static void Listen(Button b, Action onClick) => Cross.OnClick(b, onClick);

        public static Button Button(Transform parent, string label, Action onClick, bool primary = false, float width = -1, float height = 34, string name = "Button")
        {
            var img = Panel(parent, Color.white, rounded: true, name: name);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.colors = primary ? Colors(Theme.Accent, Color.Lerp(Theme.Accent, Color.white, 0.12f), Color.Lerp(Theme.Accent, Color.black, 0.15f))
                               : Colors(Theme.Raised, Theme.Hover, Theme.Pressed);
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var t = Text(img.transform, label, Theme.Body, primary ? Theme.AccentText : Theme.Text, align: TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 12, 0, 12, 0);
            Size(img, width: width >= 0 ? width : t.preferredWidth + 24, height: height);
            Listen(b, onClick);
            return b;
        }

        // Text-only button for low-emphasis actions (Reset, Clear)
        public static Button LinkButton(Transform parent, string label, Action onClick, Color? color = null)
        {
            var t = Text(parent, label, Theme.Small, color ?? Theme.TextMuted, align: TextAlignmentOptions.Center, name: "Link");
            t.raycastTarget = true;
            var b = t.gameObject.AddComponent<Button>();
            b.targetGraphic = t;
            b.colors = Colors(Color.white, new Color(1.25f, 1.25f, 1.25f), new Color(0.8f, 0.8f, 0.8f));
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            Size(t, width: t.preferredWidth + 8, height: 24);
            Listen(b, onClick);
            return b;
        }

        // The close cross drawn from two bars, not a glyph.
        public static Button CloseButton(Transform parent, Action onClick)
        {
            var img = Panel(parent, Color.white, rounded: true, name: "Close");
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.colors = Colors(new Color(1, 1, 1, 0), Theme.Hover, Theme.Pressed);
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            foreach (var angle in new[] { 45f, -45f })
            {
                var bar = Panel(img.transform, Theme.TextMuted, name: "Bar");
                bar.raycastTarget = false;
                bar.rectTransform.sizeDelta = new Vector2(14, 1.6f);
                bar.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            Size(img, width: 32, height: 32);
            Listen(b, onClick);
            return b;
        }

        // A "?" inside a thin circle, sized like the close button.
        public static Button HelpButton(Transform parent, Action onClick)
        {
            var hit = Panel(parent, Color.white, rounded: true, name: "Help");
            var b = hit.gameObject.AddComponent<Button>();
            b.targetGraphic = hit;
            b.colors = Colors(new Color(1, 1, 1, 0), Theme.Hover, Theme.Pressed);
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var ring = Panel(hit.transform, Theme.TextMuted, name: "Ring");
            ring.sprite = Theme.Ring;
            ring.raycastTarget = false;
            ring.rectTransform.sizeDelta = new Vector2(20, 20);
            var q = Text(hit.transform, "?", 13, Theme.TextMuted, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(q.rectTransform);
            Size(hit, width: 32, height: 32);
            Listen(b, onClick);
            return b;
        }

        // Hides without collapsing its layout space, so neighbouring fields don't shift.
        public static void SetShown(Component c, bool shown)
        {
            var g = c.gameObject.GetComponent<CanvasGroup>() ?? c.gameObject.AddComponent<CanvasGroup>();
            g.alpha = shown ? 1 : 0;
            g.interactable = shown;
            g.blocksRaycasts = shown;
        }

        public static Image Divider(Transform parent)
        {
            var d = Panel(parent, Theme.Line, name: "Divider");
            d.raycastTarget = false;
            Size(d, height: 1);
            return d;
        }

        static TMP_InputField Field(Transform parent, float width, string name)
        {
            var bg = Panel(parent, Theme.Raised, rounded: true, name: name);
            var area = Node("TextArea", bg.transform);
            Stretch(area, 10, 2, 10, 2);
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Text(area, "", Theme.Body, Theme.TextFaint, name: "Placeholder");
            Stretch(placeholder.rectTransform);
            var text = Text(area, "", Theme.Body, Theme.Text, name: "Value");
            Stretch(text.rectTransform);

            // added after its children exist: the field's OnEnable expects them
            var field = bg.gameObject.AddComponent<TMP_InputField>();

            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            if (Theme.Font != null) field.fontAsset = Theme.Font;
            field.pointSize = Theme.Body;
            field.caretColor = Theme.Accent;
            field.customCaretColor = true;
            field.selectionColor = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.35f);
            field.caretWidth = 2;
            field.navigation = new Navigation { mode = Navigation.Mode.None };
            field.transition = Selectable.Transition.ColorTint;
            field.targetGraphic = bg;
            field.colors = Colors(Theme.Raised, Theme.Hover, Theme.Hover);
            bg.color = Color.white;

            // stops game hotkeys (hotbar numbers, inventory, our own hotkey) while typing
            Cross.Listen<string>(field.onSelect, _ => Controls.IsTyping = true);
            Cross.Listen<string>(field.onDeselect, _ => Controls.IsTyping = false);

            Size(bg, width: width, height: 34);
            return field;
        }

        // Empty text = unset (null). Commits on Enter or when the field loses focus.
        public static TMP_InputField NumberField(Transform parent, Action<int?> onCommit, int min, int max, float width = 96)
        {
            var f = Field(parent, width, "Number");
            f.contentType = TMP_InputField.ContentType.IntegerNumber;
            f.characterLimit = max.ToString().Length;
            f.textComponent.As<TextMeshProUGUI>()!.alignment = TextAlignmentOptions.MidlineRight;
            f.placeholder.As<TextMeshProUGUI>()!.alignment = TextAlignmentOptions.MidlineRight;
            Cross.Listen<string>(f.onEndEdit, new Action<string>(value =>
            {
                Controls.IsTyping = false;
                if (string.IsNullOrWhiteSpace(value)) { onCommit(null); return; }
                if (int.TryParse(value, out var n)) onCommit(Mathf.Clamp(n, min, max));
                else onCommit(null);
            }));
            return f;
        }

        public static TMP_InputField SearchField(Transform parent, string placeholder, Action<string> onChange, float width = 240)
        {
            var f = Field(parent, width, "Search");
            f.placeholder.As<TextMeshProUGUI>()!.text = placeholder;
            Cross.Listen<string>(f.onValueChanged, onChange);
            return f;
        }

        // Shows `value` when set; otherwise empty with the inherited value as a faint placeholder.
        public static void SetNumber(TMP_InputField f, float? value, float inherited)
        {
            f.SetTextWithoutNotify(value.HasValue ? Format(value.Value) : "");
            f.placeholder.As<TextMeshProUGUI>()!.text = Format(inherited);
        }

        public static string Format(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? ((long)Mathf.Round(v)).ToString("N0") : v.ToString("N2");

        public static ScrollRect ScrollList(Transform parent, out RectTransform content)
        {
            var root = Node("Scroll", parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Node("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); // receives wheel events between rows
            hit.color = new Color(0, 0, 0, 0);

            var col = Column(viewport, name: "Content");
            content = col.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var fitter = col.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            scroll.inertia = false;
            return scroll;
        }

        // Small muted caps with a line above (except a tab's first section), so a section never reads like a row.
        // The returned text's parent is the whole header: callers toggle it via t.transform.parent.
        public static TextMeshProUGUI SectionTitle(Transform parent, string title, float topGap = 18)
        {
            bool first = topGap <= 12;
            var wrap = Column(parent, first ? 0 : 14, "Section");
            wrap.padding = new RectOffset(0, 0, first ? (int)topGap : 22, 4);
            if (!first) Divider(wrap.transform);
            var t = Text(wrap.transform, title.ToUpperInvariant(), Theme.Small, Theme.TextMuted, FontStyles.Bold);
            t.characterSpacing = 6;
            Size(t, height: 18);
            return t;
        }

        // Label with an optional muted second line, as one text: TMP nested in a flexible column didn't draw in game.
        public static TextMeshProUGUI LabelText(Transform parent, string label, string? detail, float height)
        {
            var text = detail == null ? label
                : $"{label}\n<size={Theme.Small}><color=#{ColorUtility.ToHtmlStringRGB(Theme.TextMuted)}>{detail}</color></size>";
            var t = Text(parent, text, Theme.Body, Theme.Text, name: "Label");
            t.overflowMode = TextOverflowModes.Ellipsis;
            var le = Size(t, height: height, flexWidth: 1);
            le.minWidth = 140;
            le.preferredWidth = 140;
            return t;
        }

        // Label (+ optional help line) on the left, controls on the right.
        public static Transform SettingRow(Transform parent, string label, string? help, bool phone, out TextMeshProUGUI labelText)
        {
            bool showHelp = help != null && !phone;
            var row = Row(parent, 12, showHelp ? 52 : Theme.RowHeight, "Setting:" + label);
            labelText = LabelText(row.transform, label, showHelp ? help : null, showHelp ? 44 : 22);

            var controls = Row(row.transform, 8, 34, "Controls");
            controls.childAlignment = TextAnchor.MiddleRight;
            return controls.transform;
        }

        // Text option with a white underline when selected, like the game's Phone/Character tabs.
        public static (Button button, Action<bool> setSelected) TextTab(Transform parent, string label, Action onClick, float height = 34, float fontSize = Theme.Body)
        {
            var hit = Panel(parent, new Color(0, 0, 0, 0), name: "Option:" + label);
            var b = hit.gameObject.AddComponent<Button>();
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var t = Text(hit.transform, label, fontSize, Theme.TextMuted, align: TextAlignmentOptions.Center);
            // hover brightens the label; the selected colour itself is set below
            b.targetGraphic = t;
            b.colors = Colors(Color.white, new Color(1.35f, 1.35f, 1.35f), new Color(0.85f, 0.85f, 0.85f));
            Stretch(t.rectTransform, 0, 0, 0, 3);
            var line = Panel(hit.transform, Theme.Accent, name: "Underline");
            line.raycastTarget = false;
            var lrt = line.rectTransform;
            lrt.anchorMin = new Vector2(0.5f, 0); lrt.anchorMax = new Vector2(0.5f, 0);
            lrt.pivot = new Vector2(0.5f, 0);
            lrt.sizeDelta = new Vector2(t.preferredWidth, 2);
            Size(hit, width: t.preferredWidth + 20, height: height);
            Listen(b, onClick);
            Action<bool> setSelected = on =>
            {
                t.color = on ? Theme.Text : Theme.TextMuted;
                line.gameObject.SetActive(on);
            };
            setSelected(false);
            return (b, setSelected);
        }

        // On/off switch; returns a setter for the shown state.
        public static Action<bool> Switch(Transform parent, Action<bool> onChange)
        {
            var track = Panel(parent, Theme.Raised, rounded: true, name: "Switch");
            Size(track, width: 40, height: 22);
            var knob = Panel(track.transform, Theme.TextMuted, rounded: true, name: "Knob");
            knob.raycastTarget = false;
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0, 0.5f);
            knob.rectTransform.sizeDelta = new Vector2(16, 16);
            var b = track.gameObject.AddComponent<Button>();
            b.targetGraphic = track;
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            bool state = false;
            Action<bool> show = on =>
            {
                state = on;
                track.color = on ? Theme.Accent : Theme.Raised;
                knob.color = on ? Theme.AccentText : Theme.TextMuted;
                knob.rectTransform.anchoredPosition = new Vector2(on ? 29 : 11, 0);
            };
            Listen(b, () => { show(!state); onChange(state); });
            show(false);
            return show;
        }

        // A row of underlined text options; returns a setter for the selected index (-1 = none).
        public static Action<int> Segmented(Transform parent, string[] options, Action<int> onPick)
        {
            var row = Row(parent, 4, 34, "Options");
            var setters = new Action<bool>[options.Length];
            Action<int> select = index =>
            {
                for (int i = 0; i < setters.Length; i++) setters[i](i == index);
            };
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                setters[i] = TextTab(row.transform, options[i], () => { select(index); onPick(index); }).setSelected;
            }
            return select;
        }
    }
}
