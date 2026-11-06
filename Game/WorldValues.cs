using System;
using System.Collections.Generic;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Items;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // Cash per slot, ATM limit, drying rack capacity and mixer max mix. Vanilla values are captured before the first write.
    public static class WorldValues
    {
        public const string CashPerSlot = "CashPerSlot";
        public const string AtmWeeklyLimit = "AtmWeeklyLimit";
        public const string DryingRack = "DryingRack";
        public static string MixerKey(string typeName) => "Mixer:" + typeName;

        // both are compiled-in consts (CashSlot.MAX_CASH_PER_SLOT, ATM.WeeklyDepositLimit), enforced only by these UI methods
        static readonly CodeConstant _cashPerSlot = new("Cash per slot", 1000f);
        static readonly CodeConstant _atmLimit = new("ATM weekly limit", 10000f);

        static readonly Dictionary<string, float> _vanilla = new()
        {
            [CashPerSlot] = _cashPerSlot.Vanilla,
            [AtmWeeklyLimit] = _atmLimit.Vanilla,
        };
        public static IReadOnlyDictionary<string, float> Vanilla => _vanilla;

        public static void Install()
        {
            _cashPerSlot.Install(typeof(ItemUIManager), "StartDragCash", "EndCashDrag", "AddCashAmount", "SubtractCashAmount");
            _atmLimit.Install(typeof(ATMInterface), "get_remainingAllowedDeposit", "Update", "SetSelectedAmount", "UpdateAvailableAmounts");
        }

        // cheap: no scene search
        public static void ApplyConstants(ModSettings s)
        {
            _cashPerSlot.Set(s.CashPerSlot);
            LookTarget.InvalidateCash(); // a per-case value may be due again on the next frame
            _atmLimit.Set(s.AtmWeeklyLimit);
        }

        // per-case override (LookTarget.TickCash); null = vanilla
        public static void SetCashPerSlot(float? value) => _cashPerSlot.Set(value);

        public static void Apply(ModSettings s)
        {
            ApplyConstants(s);
            foreach (var rack in UnityEngine.Object.FindObjectsOfType<DryingRack>()) ApplyTo(rack, s);
            foreach (var mixer in UnityEngine.Object.FindObjectsOfType<MixingStation>()) ApplyTo(mixer, s);
        }

        public static void ApplyTo(DryingRack rack, ModSettings s)
        {
            Capture(DryingRack, rack.ItemCapacity);
            rack.ItemCapacity = ObjectCapacity(s, rack.GUID.ToString()) ?? s.DryingRackCapacity ?? (int)_vanilla[DryingRack];
        }

        public static void ApplyTo(MixingStation mixer, ModSettings s)
        {
            var type = mixer.GetIl2CppType().Name;
            var key = MixerKey(type);
            Capture(key, mixer.MaxMixQuantity);
            mixer.MaxMixQuantity = ObjectCapacity(s, mixer.GUID.ToString())
                ?? (s.MixerMaxMix.TryGetValue(type, out var v) ? v : (int)_vanilla[key]);
        }

        static int? ObjectCapacity(ModSettings s, string guid) => s.Objects.TryGetValue(guid, out var o) ? o.Capacity : null;

        static void Capture(string key, float value)
        {
            if (!_vanilla.ContainsKey(key)) _vanilla[key] = value;
        }
    }
}
