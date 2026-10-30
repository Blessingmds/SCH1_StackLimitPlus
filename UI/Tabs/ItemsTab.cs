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

            var searchRow = UiFactory.Row(body, 8, 42, "SearchRow");
            var search = UiFactory.SearchField(searchRow.transform, "Search items", text => { _filter = text.Trim(); ApplyFilter(); }, phone ? 200 : 280);
            UiFactory.Size(search, flexWidth: 1);

            _itemList = UiFactory.Column(body, 0, "ItemList").GetComponent<RectTransform>();
            _noMatches = UiFactory.Text(body, "No items match your search.", Theme.Body, Theme.TextMuted, name: "NoMatches");
            UiFactory.Size(_noMatches, height: 40);
            AddMissingItems();
        }

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
            int shown = 0;
            foreach (var r in _items)
            {
                bool match = _filter.Length == 0 || r.Item.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
                r.Go.SetActive(match);
                if (match) shown++;
            }
            _noMatches.gameObject.SetActive(shown == 0);
        }

        public override void Refresh()
        {
            AddMissingItems();
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
