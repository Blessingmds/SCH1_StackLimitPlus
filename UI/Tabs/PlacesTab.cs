using System;
using System.Collections.Generic;
#if MONO
using TMPro;
#else
using Il2CppTMPro;
#endif
using MDSAPI.Unity;
using SCH1_StackLimitPlus.Settings;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    public class PlacesTab : Tab
    {
        static readonly (Place place, string name, string help)[] Rows =
        {
            (Place.Pockets, "Pockets", "Your inventory and hotbar"),
            (Place.Storage, "Storage", "Shelves, safes and racks"),
            (Place.Vehicles, "Vehicle trunks", "Every vehicle; change one type below"),
            (Place.Deliveries, "Deliveries", "Delivery vans; higher lets you order more at once"),
            (Place.Npcs, "Employees & dealers", "Their own inventories"),
            (Place.DryingRack, "Drying rack", "Vanilla keeps one batch per slot"),
            (Place.BrickPress, "Brick press", "Vanilla keeps one batch per slot"),
            (Place.Cauldron, "Cauldron", "Vanilla keeps one batch per slot"),
            (Place.ChemistryStation, "Chemistry station", "Vanilla keeps one batch per slot"),
            (Place.LabOven, "Lab oven", "Vanilla keeps one batch per slot"),
            (Place.MixingStation, "Mixing station", "Vanilla keeps one batch per slot"),
            (Place.PackagingStation, "Packaging station", "Vanilla keeps one batch per slot"),
        };

        static readonly (PlacePreset preset, string label)[] Presets =
        {
            (PlacePreset.PocketsOnly, "Pockets only"),
            (PlacePreset.ExceptStations, "All but stations"),
            (PlacePreset.Everywhere, "Everywhere"),
        };

        readonly Dictionary<Place, (Action<int> select, TMP_InputField field)> _rows = new();
        readonly Dictionary<string, (Action<int> select, TMP_InputField field)> _vehicleRows = new();
        Action<int> _selectPreset = null!;
        UnityEngine.RectTransform _vehicles = null!;
        bool _phone;

        public override string Title => "Places";

        public override void Build(UnityEngine.RectTransform content, bool phone)
        {
            _phone = phone;
            var body = ScrollBody(content, phone);

            UiFactory.SectionTitle(body, "Where limits apply", 12);
            var intro = UiFactory.Text(body, "Item limits uses the Items tab. Vanilla keeps the game's own stack size. Custom uses one number for every item there.",
                Theme.Small, Theme.TextMuted);
            intro.enableWordWrapping = true;

            var presets = UiFactory.SettingRow(body, "Preset", null, phone, out _);
            var labels = Array.ConvertAll(Presets, p => p.label);
            _selectPreset = UiFactory.Segmented(presets, labels, i => Commit(s => PlacePresets.Apply(s, Presets[i].preset)));

            UiFactory.SectionTitle(body, "Inventories");
            foreach (var (place, name, help) in Rows)
            {
                if (place == Place.DryingRack)
                {
                    // filled on refresh: the game's vehicle list only exists once a save is loaded
                    _vehicles = UiFactory.Column(body, 0, "VehicleTypes").GetComponent<UnityEngine.RectTransform>();
                    UiFactory.SectionTitle(body, "Stations");
                }
                var controls = UiFactory.SettingRow(body, name, LimitResolver.IsStation(place) ? null : help, phone, out _);
                var p = place;
                var select = UiFactory.Segmented(controls, new[] { "Item limits", "Vanilla", "Custom" },
                    i => Commit(s => s.Places[p].Mode = (PlaceMode)i));
                var field = UiFactory.NumberField(controls, v => Commit(s =>
                {
                    s.Places[p].Mode = PlaceMode.Custom;
                    if (v.HasValue) s.Places[p].Custom = v.Value;
                }), 1, SettingsStore.MaxStack, 84);
                _rows[place] = (select, field);
            }

            var note = UiFactory.Text(body, "Stations on vanilla let workers spread batches over every slot, so nothing gets halved.",
                Theme.Small, Theme.TextMuted);
            note.enableWordWrapping = true;
        }

        void BuildVehicleRows()
        {
            var types = Game.VehicleTypes.All();
            if (types.Count == 0) return;
            UiFactory.SectionTitle(_vehicles, "Vehicle types");
            foreach (var (code, name) in types)
            {
                var key = ObjectSettings.VehicleKey(code);
                var controls = UiFactory.SettingRow(_vehicles, name, null, _phone, out _);
                var select = UiFactory.Segmented(controls, new[] { "Same as trunks", "Item limits", "Vanilla", "Custom" },
                    i => Commit(s => ObjectSettings.Update(s, key, o => o.SlotMode = (ObjectSlotMode)i)));
                var field = UiFactory.NumberField(controls, v => Commit(s => ObjectSettings.Update(s, key, o =>
                {
                    o.SlotMode = ObjectSlotMode.Custom;
                    if (v.HasValue) o.SlotCustom = v.Value;
                })), 1, SettingsStore.MaxStack, 84);
                _vehicleRows[key] = (select, field);
            }
        }

        public override void Refresh()
        {
            var s = Core.Settings;
            var current = PlacePresets.Current(s);
            _selectPreset(current.HasValue ? Array.FindIndex(Presets, p => p.preset == current.Value) : -1);
            foreach (var (place, (select, field)) in _rows)
            {
                var ps = s.Places[place];
                select((int)ps.Mode);
                ShowField(field, ps.Custom, ps.Mode == PlaceMode.Custom);
            }

            if (_vehicleRows.Count == 0) BuildVehicleRows();
            foreach (var (key, (select, field)) in _vehicleRows)
            {
                var o = ObjectSettings.Get(s, key);
                select((int)o.SlotMode);
                ShowField(field, o.SlotCustom, o.SlotMode == ObjectSlotMode.Custom);
            }
        }

        static void ShowField(TMP_InputField field, int value, bool shown)
        {
            field.SetTextWithoutNotify(value.ToString());
            field.placeholder.As<TextMeshProUGUI>()!.text = "100";
            UiFactory.SetShown(field, shown);
            field.interactable = !ReadOnly;
            foreach (var b in field.transform.parent.GetComponentsInChildren<Button>()) b.interactable = !ReadOnly;
        }
    }
}
