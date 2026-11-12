using System;
using System.Collections.Generic;
#if MONO
using TMPro;
#else
using Il2CppTMPro;
#endif
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    public class CashTab : Tab
    {
        readonly List<(string key, Func<ModSettings, float?> get, TMP_InputField field, Button reset)> _rows = new();

        public override string Title => "Cash";

        public override void Build(RectTransform content, bool phone)
        {
            var body = ScrollBody(content, phone);
            UiFactory.SectionTitle(body, "Storage", 12);
            Add(body, phone, WorldValues.CashPerSlot, "Cash per slot", "How much cash fits in one storage slot, like employee beds and storage racks",
                s => s.CashPerSlot, (s, v) => s.CashPerSlot = v);
            UiFactory.SectionTitle(body, "ATM");
            Add(body, phone, WorldValues.AtmWeeklyLimit, "Weekly deposit limit", "How much you can deposit at ATMs each week",
                s => s.AtmWeeklyLimit, (s, v) => s.AtmWeeklyLimit = v);
        }

        void Add(RectTransform body, bool phone, string key, string label, string help, Func<ModSettings, float?> get, Action<ModSettings, float?> set)
        {
            var controls = UiFactory.SettingRow(body, label, help, phone, out _);
            var reset = UiFactory.LinkButton(controls, "Reset", () => Commit(s => set(s, null)));
            var field = UiFactory.NumberField(controls, v => Commit(s => set(s, v)), 1, (int)SettingsStore.MaxCash, 120);
            _rows.Add((key, get, field, reset));
        }

        public override void Refresh()
        {
            foreach (var (key, get, field, reset) in _rows)
            {
                var value = get(Core.Settings);
                UiFactory.SetNumber(field, value, WorldValues.Vanilla.TryGetValue(key, out var v) ? v : 0);
                field.interactable = !ReadOnly;
                UiFactory.SetShown(reset, value.HasValue && !ReadOnly);
            }
        }
    }
}
