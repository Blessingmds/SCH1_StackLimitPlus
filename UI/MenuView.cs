using System;
using System.Collections.Generic;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI
{
    // The menu itself: header, sidebar tabs, content, footer. Hosted by the F6 overlay or the phone app.
    public class MenuView
    {
        public const string Version = "2.0.0";

        readonly bool _phone;
        readonly RectTransform _sidebar;
        readonly RectTransform _contentArea;
        readonly TextMeshProUGUI _footerLeft;
        readonly TextMeshProUGUI _footerRight;
        readonly List<(Tab tab, Button button, RectTransform? content)> _tabs = new();
        int _current = -1;

        public RectTransform Root { get; }
        public RectTransform Header { get; }
        public string? CurrentTitle => _current >= 0 ? _tabs[_current].tab.Title : null;

        public MenuView(RectTransform root, bool phone, Action? onClose)
        {
            Root = root;
            _phone = phone;
            // phone: tabs in a strip under the header (mobile layout), no footer; desktop: sidebar + footer
            float headerH = phone ? 36 : 52, footerH = phone ? 0 : 32, sidebarW = phone ? 0 : 176, tabStripH = phone ? 34 : 0;

            var border = UiFactory.Panel(root, Theme.Line, name: "Border");
            UiFactory.Stretch(border.rectTransform);
            var window = UiFactory.Panel(border.transform, Theme.Window, name: "Window");
            UiFactory.Stretch(window.rectTransform, 1, 1, 1, 1);

            Header = UiFactory.Node("Header", window.transform);
            Header.anchorMin = new Vector2(0, 1); Header.anchorMax = new Vector2(1, 1); Header.pivot = new Vector2(0.5f, 1);
            Header.offsetMin = new Vector2(0, -headerH); Header.offsetMax = Vector2.zero;
            var headerRow = Header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerRow.padding = new RectOffset((int)Theme.Pad, phone ? 12 : 10, 0, 0);
            headerRow.spacing = 10;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true; headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false; headerRow.childForceExpandHeight = false;
            var title = UiFactory.Text(Header, "StackLimit Plus", phone ? 17 : Theme.Title, Theme.Text);
            UiFactory.Size(title, width: title.preferredWidth + 2, height: 28);
            var version = UiFactory.Text(Header, Version, Theme.Small, Theme.TextFaint);
            UiFactory.Size(version, width: version.preferredWidth + 2, height: 28);
            var spacer = UiFactory.Node("Spacer", Header);
            UiFactory.Size(spacer, height: 1, flexWidth: 1);
            if (onClose != null) UiFactory.CloseButton(Header, onClose);

            var headerLine = UiFactory.Panel(window.transform, Theme.Line, name: "HeaderLine");
            headerLine.rectTransform.anchorMin = new Vector2(0, 1); headerLine.rectTransform.anchorMax = new Vector2(1, 1);
            headerLine.rectTransform.offsetMin = new Vector2(0, -headerH - 1); headerLine.rectTransform.offsetMax = new Vector2(0, -headerH);

            var sidebarBg = UiFactory.Panel(window.transform, Theme.Sidebar, name: phone ? "TabStrip" : "Sidebar");
            _sidebar = sidebarBg.rectTransform;
            if (phone)
            {
                _sidebar.anchorMin = new Vector2(0, 1); _sidebar.anchorMax = new Vector2(1, 1);
                _sidebar.offsetMin = new Vector2(0, -headerH - 1 - tabStripH); _sidebar.offsetMax = new Vector2(0, -headerH - 1);
                var strip = _sidebar.gameObject.AddComponent<HorizontalLayoutGroup>();
                strip.padding = new RectOffset(8, 8, 0, 0);
                strip.childAlignment = TextAnchor.MiddleLeft;
                strip.childControlWidth = true; strip.childControlHeight = true;
                strip.childForceExpandWidth = false; strip.childForceExpandHeight = false;
                var stripLine = UiFactory.Panel(window.transform, Theme.Line, name: "TabStripLine");
                stripLine.rectTransform.anchorMin = new Vector2(0, 1); stripLine.rectTransform.anchorMax = new Vector2(1, 1);
                stripLine.rectTransform.offsetMin = new Vector2(0, -headerH - 2 - tabStripH); stripLine.rectTransform.offsetMax = new Vector2(0, -headerH - 1 - tabStripH);
            }
            else
            {
                _sidebar.anchorMin = Vector2.zero; _sidebar.anchorMax = new Vector2(0, 1);
                _sidebar.offsetMin = new Vector2(0, footerH); _sidebar.offsetMax = new Vector2(sidebarW, -headerH - 1);
                var tabs = _sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
                tabs.padding = new RectOffset((int)Theme.Pad - 4, 8, 12, 10);
                tabs.spacing = 0;
                tabs.childControlWidth = true; tabs.childControlHeight = true;
                tabs.childForceExpandWidth = true; tabs.childForceExpandHeight = false;
                var sideLine = UiFactory.Panel(window.transform, Theme.Line, name: "SidebarLine");
                sideLine.rectTransform.anchorMin = Vector2.zero; sideLine.rectTransform.anchorMax = new Vector2(0, 1);
                sideLine.rectTransform.offsetMin = new Vector2(sidebarW, footerH + 1); sideLine.rectTransform.offsetMax = new Vector2(sidebarW + 1, -headerH - 1);
            }

            _contentArea = UiFactory.Node("Content", window.transform);
            _contentArea.anchorMin = Vector2.zero; _contentArea.anchorMax = Vector2.one;
            _contentArea.offsetMin = new Vector2(sidebarW, footerH); _contentArea.offsetMax = new Vector2(0, -headerH - 2 - tabStripH);

            var footerLine = UiFactory.Panel(window.transform, Theme.Line, name: "FooterLine");
            footerLine.rectTransform.anchorMin = Vector2.zero; footerLine.rectTransform.anchorMax = new Vector2(1, 0);
            footerLine.rectTransform.offsetMin = new Vector2(0, footerH); footerLine.rectTransform.offsetMax = new Vector2(0, footerH + 1);
            _footerLeft = UiFactory.Text(window.transform, "", Theme.Small, Theme.TextMuted, name: "FooterLeft");
            Anchor(_footerLeft.rectTransform, footerH, TextAlignmentOptions.MidlineLeft, _footerLeft);
            _footerRight = UiFactory.Text(window.transform, "", Theme.Small, Theme.Accent, name: "FooterRight");
            Anchor(_footerRight.rectTransform, footerH, TextAlignmentOptions.MidlineRight, _footerRight);
            foreach (var go in new[] { footerLine.gameObject, _footerLeft.gameObject, _footerRight.gameObject }) go.SetActive(!phone);
        }

        static void Anchor(RectTransform rt, float h, TextAlignmentOptions align, TextMeshProUGUI t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(Theme.Pad, 0); rt.offsetMax = new Vector2(-Theme.Pad, h);
            t.alignment = align;
        }

        public void AddTab(Tab tab)
        {
            if (_phone && !tab.PhoneVisible) return;
            int index = _tabs.Count;
            var (b, setSelected) = UiFactory.TextTab(_sidebar, tab.Title, () => Show(index), _phone ? 34 : 40, _phone ? 15 : 16);
            if (!_phone)
            {
                // sidebar tabs read left-aligned, underline under the word
                var label = b.GetComponentInChildren<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.BottomLeft;
                UiFactory.Stretch(label.rectTransform, 0, 0, 0, 8);
                var line = b.transform.Find("Underline").GetComponent<RectTransform>();
                line.anchorMin = line.anchorMax = line.pivot = new Vector2(0, 0);
                line.anchoredPosition = new Vector2(0, 2);
            }
            _tabs.Add((tab, b, null));
            _setSelected.Add(setSelected);
        }

        readonly List<Action<bool>> _setSelected = new();

        void StyleTab(int i, bool selected) => _setSelected[i](selected);

        public void Show(string title)
        {
            int i = _tabs.FindIndex(t => t.tab.Title == title);
            Show(i >= 0 && _tabs[i].tab.Available ? i : FirstAvailable());
        }

        int FirstAvailable() => Math.Max(0, _tabs.FindIndex(t => t.tab.Available));

        void Show(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var (tab, b, content) = _tabs[i];
                b.gameObject.SetActive(tab.Available);
                if (content != null) content.gameObject.SetActive(i == index);
                StyleTab(i, i == index);
            }
            var entry = _tabs[index];
            if (entry.content == null)
            {
                var content = UiFactory.Node("Tab:" + entry.tab.Title, _contentArea);
                UiFactory.Stretch(content);
                try { entry.tab.Build(content, _phone); }
                catch (Exception ex) { Core.ErrorOnce($"Building tab {entry.tab.Title} failed: {ex}"); }
                _tabs[index] = (entry.tab, entry.button, content);
            }
            _current = index;
            Refresh();
        }

        public void Refresh()
        {
            if (_current < 0) return;
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].button.gameObject.SetActive(_tabs[i].tab.Available);
            if (!_tabs[_current].tab.Available) { Show(FirstAvailable()); return; }
            try { _tabs[_current].tab.Refresh(); }
            catch (Exception ex) { Core.ErrorOnce($"Refreshing tab {_tabs[_current].tab.Title} failed: {ex}"); }
            _footerLeft.text = _phone
                ? $"{Core.Catalog.Items.Count} items"
                : $"{Core.Settings.Hotkey} to close  ·  {Core.Catalog.Items.Count} items";
            _footerRight.text = Core.IsClient ? "Host's settings (read-only)" : "";
        }
    }
}
