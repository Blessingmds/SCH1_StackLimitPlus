using System;
using System.Collections.Generic;
using Il2CppTMPro;
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
            UiFactory.SectionTitle(body, "ATM", 12);
            Add(body, phone, WorldValues.AtmWeeklyLimit, "Weekly deposit limit", "How much you can deposit at ATMs each week. Can only be raised.",
                s => s.AtmWeeklyLimit, (s, v) => s.AtmWeeklyLimit = v);

            var note = UiFactory.Text(body, "Cash per slot (1,000) is fixed inside the game's code and can't be changed by a mod.",
                Theme.Small, Theme.TextMuted);
            note.enableWordWrapping = true;
            note.overflowMode = Il2CppTMPro.TextOverflowModes.Overflow;
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
