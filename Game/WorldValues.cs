using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ObjectScripts;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // Cash statics, drying rack capacity and mixer max mix. Vanilla values are captured before the first write.
    public static class WorldValues
    {
        public const string CashPerSlot = "CashPerSlot";
        public const string MaxBalance = "MaxBalance";
        public const string AtmWeeklyLimit = "AtmWeeklyLimit";
        public const string DryingRack = "DryingRack";
        public static string MixerKey(string typeName) => "Mixer:" + typeName;

        static readonly Dictionary<string, float> _vanilla = new();
        static readonly HashSet<string> _written = new();
        public static IReadOnlyDictionary<string, float> Vanilla => _vanilla;

        // Il2Cpp statics live in memory created by the class initializer; touching them before it ran crashes natively.
        static void EnsureInit<T>() => IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<T>.NativeClassPtr);

        public static void CaptureStatics()
        {
            EnsureInit<CashSlot>();
            EnsureInit<CashInstance>();
            EnsureInit<ATM>();
            Capture(CashPerSlot, CashSlot.MAX_CASH_PER_SLOT);
            Capture(MaxBalance, CashInstance.MAX_BALANCE);
            Capture(AtmWeeklyLimit, ATM.WeeklyDepositLimit);
        }

        public static void Apply(ModSettings s)
        {
            CaptureStatics();
            // only write what the user set, or what we changed before and must restore
            if (ShouldWrite(CashPerSlot, s.CashPerSlot)) CashSlot.MAX_CASH_PER_SLOT = s.CashPerSlot ?? _vanilla[CashPerSlot];
            if (ShouldWrite(MaxBalance, s.MaxBalance)) CashInstance.MAX_BALANCE = s.MaxBalance ?? _vanilla[MaxBalance];
            if (ShouldWrite(AtmWeeklyLimit, s.AtmWeeklyLimit)) ATM.WeeklyDepositLimit = s.AtmWeeklyLimit ?? _vanilla[AtmWeeklyLimit];

            foreach (var rack in UnityEngine.Object.FindObjectsOfType<DryingRack>()) ApplyTo(rack, s);
            foreach (var mixer in UnityEngine.Object.FindObjectsOfType<MixingStation>()) ApplyTo(mixer, s);
        }

        static bool ShouldWrite(string key, float? value)
        {
            if (value.HasValue) { _written.Add(key); return true; }
            return _written.Remove(key);
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
