using Il2CppTMPro;
using SCH1_StackLimitPlus.Game;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace SCH1_StackLimitPlus.UI.Tabs
{
    // Settings for the one rack/station/storage the player was looking at when opening the menu.
    public class ObjectTab : Tab
    {
        TextMeshProUGUI _name = null!;
        TextMeshProUGUI _place = null!;
        System.Action<int> _selectMode = null!;
        TMP_InputField _slotCustom = null!;
        GameObject _capacityRow = null!;
        TMP_InputField _capacity = null!;
        Button _capacityReset = null!;
        TextMeshProUGUI _count = null!;
        GameObject _cashTitle = null!, _slotTitle = null!;
        readonly System.Collections.Generic.List<GameObject> _slotSection = new();
        TMP_InputField _cash = null!;
        Button _cashReset = null!;

        public override string Title => "This object";
        public override bool PhoneVisible => false;
        public override bool Available => Core.OpenTarget != null;

        static Target? T => Core.OpenTarget;

        public override void Build(RectTransform content, bool phone)
        {
            var body = ScrollBody(content, phone);
            _name = UiFactory.SectionTitle(body, "", 12);
            _place = UiFactory.Text(body, "", Theme.Small, Theme.TextMuted);
            UiFactory.Size(_place, height: 20);

            // a worker's bed: its case only holds cash, so it shows cash per slot instead of the stack size rows
            _cashTitle = UiFactory.SectionTitle(body, "Cash").transform.parent.gameObject;
            var cashRow = UiFactory.SettingRow(body, "Cash per slot", "How much cash fits in one slot of this case only", phone, out _);
            _cashReset = UiFactory.LinkButton(cashRow, "Reset", () => Edit(o => o.CashPerSlot = null));
            _cash = UiFactory.NumberField(cashRow, v => Edit(o => o.CashPerSlot = v), 1, (int)SettingsStore.MaxCash, 120);

            _slotTitle = UiFactory.SectionTitle(body, "Slot stack size").transform.parent.gameObject;
            var mode = UiFactory.SettingRow(body, "Stack size", "Overrides the Places setting for this object only", phone, out _);
            _selectMode = UiFactory.Segmented(mode, new[] { "Follow place", "Item limits", "Vanilla", "Custom" },
                i => Edit(o => o.SlotMode = (ObjectSlotMode)i));
            var customRow = UiFactory.SettingRow(body, "Custom size", null, phone, out _);
            _slotCustom = UiFactory.NumberField(customRow, v => Edit(o =>
            {
                o.SlotMode = ObjectSlotMode.Custom;
                if (v.HasValue) o.SlotCustom = v.Value;
            }), 1, SettingsStore.MaxStack);
            _slotSection.Add(mode.parent.gameObject);
            _slotSection.Add(customRow.parent.gameObject);

            _capacityRow = UiFactory.SectionTitle(body, "Capacity").transform.parent.gameObject;
            var capRow = UiFactory.SettingRow(body, "Capacity", "How many items this one works on at once", phone, out _);
            _capacityReset = UiFactory.LinkButton(capRow, "Reset", () => Edit(o => o.Capacity = null));
            _capacity = UiFactory.NumberField(capRow, v => Edit(o => o.Capacity = v), 1, SettingsStore.MaxStack);

            var footer = UiFactory.Row(body, 12, 44, "Customised");
            _count = UiFactory.Text(footer.transform, "", Theme.Small, Theme.TextMuted);
            UiFactory.Size(_count, flexWidth: 1, height: 20);
            UiFactory.LinkButton(footer.transform, "Clear all objects", () => Commit(s => s.Objects.Clear()));
        }

        static void Edit(System.Action<ObjectSetting> change)
        {
            if (T == null) return;
            Commit(s => ObjectSettings.Update(s, T.Guid, change));
        }

        public override void Refresh()
        {
            if (T == null) return;
            var s = Core.Settings;
            var o = ObjectSettings.Get(s, T.Guid);
            _name.text = T.Name;
            _place.text = T.CashCase ? "A worker's cash case" : $"Counts as {PlaceName(T.Place)} in Places";

            bool cash = T.CashCase;
            _cashTitle.SetActive(cash);
            _cash.transform.parent.parent.gameObject.SetActive(cash);
            _slotTitle.SetActive(!cash);
            _slotSection.ForEach(g => g.SetActive(!cash));
            if (cash)
            {
                UiFactory.SetNumber(_cash, o.CashPerSlot, s.CashPerSlot ?? WorldValues.Vanilla[WorldValues.CashPerSlot]);
                _cash.interactable = !ReadOnly;
                UiFactory.SetShown(_cashReset, o.CashPerSlot.HasValue && !ReadOnly);
            }
            _selectMode((int)o.SlotMode);
            _slotCustom.SetTextWithoutNotify(o.SlotCustom.ToString());
            UiFactory.SetShown(_slotCustom.transform.parent.parent, o.SlotMode == ObjectSlotMode.Custom);
            _slotCustom.interactable = !ReadOnly;

            _capacityRow.SetActive(T.HasCapacity);
            _capacity.transform.parent.parent.gameObject.SetActive(T.HasCapacity);
            if (T.HasCapacity)
            {
                float inherited = T.Place == Place.DryingRack
                    ? s.DryingRackCapacity ?? (WorldValues.Vanilla.TryGetValue(WorldValues.DryingRack, out var r) ? r : 0)
                    : MixerInherited(s);
                UiFactory.SetNumber(_capacity, o.Capacity, inherited);
                _capacity.interactable = !ReadOnly;
                UiFactory.SetShown(_capacityReset, o.Capacity.HasValue && !ReadOnly);
            }

            int n = s.Objects.Count;
            _count.text = n == 1 ? "1 object customised" : $"{n} objects customised";
        }

        static float MixerInherited(ModSettings s)
        {
            var type = T!.Item.GetIl2CppType().Name;
            if (s.MixerMaxMix.TryGetValue(type, out var v)) return v;
            return WorldValues.Vanilla.TryGetValue(WorldValues.MixerKey(type), out var vanilla) ? vanilla : 0;
        }

        static string PlaceName(Place p) => p switch
        {
            Place.DryingRack => "Drying rack",
            Place.BrickPress => "Brick press",
            Place.ChemistryStation => "Chemistry station",
            Place.LabOven => "Lab oven",
            Place.MixingStation => "Mixing station",
            Place.PackagingStation => "Packaging station",
            _ => p.ToString(),
        };
    }
}
