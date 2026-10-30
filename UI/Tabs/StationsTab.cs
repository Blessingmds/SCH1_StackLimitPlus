using System;
using System.Collections.Generic;
using Il2CppTMPro;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    public class StationsTab : Tab
    {
        static readonly (string type, string name)[] Mixers =
        {
            ("MixingStation", "Mixing station"),
            ("MixingStationMk2", "Mixing station Mk2"),
        };

        readonly List<(string vanillaKey, Func<ModSettings, int?> get, TMP_InputField field, Button reset)> _rows = new();

        public override string Title => "Stations";

        public override void Build(RectTransform content, bool phone)
        {
            var body = ScrollBody(content, phone);
            UiFactory.SectionTitle(body, "Capacity", 12);
            Add(body, phone, WorldValues.DryingRack, "Drying rack", "How many items one rack dries at once",
                s => s.DryingRackCapacity, (s, v) => s.DryingRackCapacity = v);
            foreach (var (type, name) in Mixers)
            {
                var t = type;
                Add(body, phone, WorldValues.MixerKey(type), name, "Most items mixed in one go",
                    s => s.MixerMaxMix.TryGetValue(t, out var v) ? v : null,
                    (s, v) => { if (v.HasValue) s.MixerMaxMix[t] = v.Value; else s.MixerMaxMix.Remove(t); });
            }

            var note = UiFactory.Text(body, "Other stations have no capacity of their own. Set how much fits in their slots in Places. To change one station only, look at it and press "
                + Core.Settings.Hotkey + ".", Theme.Small, Theme.TextMuted);
            note.enableWordWrapping = true;
        }

        void Add(RectTransform body, bool phone, string vanillaKey, string label, string help, Func<ModSettings, int?> get, Action<ModSettings, int?> set)
        {
            var controls = UiFactory.SettingRow(body, label, help, phone, out _);
            var reset = UiFactory.LinkButton(controls, "Reset", () => Commit(s => set(s, null)));
            var field = UiFactory.NumberField(controls, v => Commit(s => set(s, v)), 1, SettingsStore.MaxStack);
            _rows.Add((vanillaKey, get, field, reset));
        }

        public override void Refresh()
        {
            foreach (var (key, get, field, reset) in _rows)
            {
                var value = get(Core.Settings);
                bool known = WorldValues.Vanilla.TryGetValue(key, out var vanilla);
                UiFactory.SetNumber(field, value, vanilla);
                if (!known) field.placeholder.Cast<TextMeshProUGUI>().text = "Vanilla";
                field.interactable = !ReadOnly;
                UiFactory.SetShown(reset, value.HasValue && !ReadOnly);
            }
        }
    }
}
