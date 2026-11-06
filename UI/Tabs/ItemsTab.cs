using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppTMPro;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    public class ItemsTab : Tab
    {
        static readonly string[] Categories =
        {
            "Product", "Packaging", "Agriculture", "Ingredient", "Consumable", "Tools", "Equipment",
            "Furniture", "Lighting", "Decoration", "Storage", "Clothing", "Cash",
        };

        class ValueRow
        {
            public TMP_InputField Field = null!;
            public Button Reset = null!;
        }

        class ItemRow : ValueRow
        {
            public CatalogItem Item = null!;
            public GameObject Go = null!;
        }

        readonly ValueRow _global = new();
        readonly Dictionary<string, ValueRow> _categories = new();
        readonly List<ItemRow> _items = new();
        readonly HashSet<string> _builtIds = new();
        Action<bool> _showUnstackable = null!;
        RectTransform _itemList = null!;
        TextMeshProUGUI _noMatches = null!;
        string _filter = "";
        bool _phone;

        public override string Title => "Items";

        public override void Build(RectTransform content, bool phone)
        {
            _phone = phone;
            var body = ScrollBody(content, phone);

            UiFactory.SectionTitle(body, "All items", 12);
            var controls = UiFactory.SettingRow(body, "Stack limit", "Used by every item unless a category or the item has its own", phone, out _);
            AddValue(_global, controls, v => Commit(s => s.Global = v));

            UiFactory.SectionTitle(body, "Categories");
            foreach (var cat in Categories)
            {
                var row = new ValueRow();
                var c = UiFactory.SettingRow(body, cat, null, phone, out _);
                AddValue(row, c, v => Commit(s => { if (v.HasValue) s.Categories[cat] = v.Value; else s.Categories.Remove(cat); }));
                _categories[cat] = row;
            }

            UiFactory.SectionTitle(body, "Items");
            var unstackable = UiFactory.SettingRow(body, "Include unstackable items",
                "Guns, tools and magazines normally keep their vanilla size", phone, out _);
            _showUnstackable = UiFactory.Switch(unstackable, on => Commit(s => s.IncludeUnstackable = on));

            // chips wrap onto as many lines as the width needs
            var chips = UiFactory.Node("GroupFilter", body);
            var grid = chips.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(phone ? 96 : 112, 32);
            grid.spacing = new Vector2(4, 2);
            grid.padding = new RectOffset(0, 0, 4, 6);
            _chips = chips;

            var searchRow = UiFactory.Row(body, 8, 42, "SearchRow");
            var search = UiFactory.SearchField(searchRow.transform, "Search items", text => { _filter = text.Trim(); _page = 0; ApplyFilter(); }, phone ? 200 : 280);
            UiFactory.Size(search, flexWidth: 1);

            _itemList = UiFactory.Column(body, 0, "ItemList").GetComponent<RectTransform>();
            _noMatches = UiFactory.Text(body, "No items match your search.", Theme.Body, Theme.TextMuted, name: "NoMatches");
            UiFactory.Size(_noMatches, height: 40);

            _pager = UiFactory.Row(body, 12, 48, "Pager").gameObject;
            _pager.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            _prev = UiFactory.Button(_pager.transform, "‹ Prev", () => { _page--; ApplyFilter(); }, width: 90);
            _pageText = UiFactory.Text(_pager.transform, "", Theme.Small, Theme.TextMuted);
            _pageText.alignment = TextAlignmentOptions.Center;
            UiFactory.Size(_pageText, width: 110, height: 34);
            _next = UiFactory.Button(_pager.transform, "Next ›", () => { _page++; ApplyFilter(); }, width: 90);
            AddMissingItems();
        }

        RectTransform _chips = null!;
        string? _group; // null = all
        string _chipKey = "";
        readonly List<(string? group, Action<bool> setSelected)> _chipViews = new();

        // drug types first, then the game's categories in menu order
        void BuildChips()
        {
            var present = new HashSet<string>(_items.Select(r => r.Item.Group));
            var groups = present.Where(g => !Categories.Contains(g)).OrderBy(g => g)
                .Concat(Categories.Where(present.Contains)).ToList();
            var key = string.Join("|", groups);
            if (key == _chipKey) return;
            _chipKey = key;
            for (int i = _chips.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_chips.GetChild(i).gameObject);
            _chipViews.Clear();
            foreach (var g in groups.Prepend(null))
            {
                var group = g;
                var (_, setSelected) = UiFactory.TextTab(_chips, group ?? "All", () => { _group = group; _page = 0; ApplyFilter(); }, 32, Theme.Small);
                _chipViews.Add((group, setSelected));
            }
            if (_group != null && !present.Contains(_group)) _group = null;
        }

        const int PageSize = 25;
        int _page;
        GameObject _pager = null!;
        Button _prev = null!, _next = null!;
        TextMeshProUGUI _pageText = null!;

        static void AddValue(ValueRow row, Transform controls, Action<int?> commit)
        {
            row.Reset = UiFactory.LinkButton(controls, "Reset", () => commit(null));
            row.Field = UiFactory.NumberField(controls, commit, 1, SettingsStore.MaxStack);
        }

        void AddMissingItems()
        {
            foreach (var item in Core.Catalog.Items)
            {
                if (!_builtIds.Add(item.Info.Id)) continue;
                var r = new ItemRow { Item = item };
                var row = UiFactory.Row(_itemList, 12, 48, "Item:" + item.Info.Id);
                r.Go = row.gameObject;

                var iconBox = UiFactory.Panel(row.transform, Theme.Raised, rounded: true, name: "IconBox");
                iconBox.raycastTarget = false;
                UiFactory.Size(iconBox, width: 34, height: 34);
                if (item.Icon != null)
                {
                    var icon = UiFactory.Panel(iconBox.transform, Color.white, name: "Icon");
                    icon.sprite = item.Icon;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    UiFactory.Stretch(icon.rectTransform, 3, 3, 3, 3);
                }

                var detail = item.Info.Excluded
                    ? (_phone ? "own count" : $"{item.Info.Category} · has its own count, set individually")
                    : $"{item.Info.Category} · vanilla {item.Info.Vanilla}";
                UiFactory.LabelText(row.transform, item.Name, detail, 44);

                var controls = UiFactory.Row(row.transform, 8, 34, "Controls");
                var id = item.Info.Id;
                AddValue(r, controls.transform, v => Commit(s => { if (v.HasValue) s.Items[id] = v.Value; else s.Items.Remove(id); }));
                _items.Add(r);
            }
        }

        void ApplyFilter()
        {
            foreach (var (group, setSelected) in _chipViews) setSelected(group == _group);
            var matches = _items.Where(r => (_group == null || r.Item.Group == _group)
                && (_filter.Length == 0 || r.Item.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            int pages = Math.Max(1, (matches.Count + PageSize - 1) / PageSize);
            _page = Math.Clamp(_page, 0, pages - 1);
            var visible = new HashSet<ItemRow>(matches.Skip(_page * PageSize).Take(PageSize));
            foreach (var r in _items) r.Go.SetActive(visible.Contains(r));

            _noMatches.gameObject.SetActive(matches.Count == 0);
            _pager.SetActive(pages > 1);
            _pageText.text = $"Page {_page + 1} of {pages}";
            _prev.interactable = _page > 0;
            _next.interactable = _page < pages - 1;
        }

        public override void Refresh()
        {
            AddMissingItems();
            BuildChips();
            var s = Core.Settings;

            SetRow(_global, s.Global, null, "Vanilla");
            foreach (var (cat, row) in _categories)
                SetRow(row, s.Categories.TryGetValue(cat, out var c) ? c : null, s.Global, "Vanilla");
            _showUnstackable(s.IncludeUnstackable);

            foreach (var r in _items)
            {
                var info = r.Item.Info;
                SetRow(r, s.Items.TryGetValue(info.Id, out var v) ? v : null, LimitResolver.InheritedItemLimit(s, info), null);
            }
            ApplyFilter();
        }

        static void SetRow(ValueRow row, int? value, int? inherited, string? inheritedText)
        {
            UiFactory.SetNumber(row.Field, value, inherited ?? 0);
            if (!inherited.HasValue && inheritedText != null) row.Field.placeholder.Cast<TextMeshProUGUI>().text = inheritedText;
            row.Field.interactable = !ReadOnly;
            UiFactory.SetShown(row.Reset, value.HasValue && !ReadOnly);
        }
    }
}
