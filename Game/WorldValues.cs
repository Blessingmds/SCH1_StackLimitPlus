using System;
using System.Collections.Generic;
using MelonLoader;
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
            if (ShouldWrite(CashPerSlot, s.CashPerSlot)) SetStatic<CashSlot>("MAX_CASH_PER_SLOT", s.CashPerSlot ?? _vanilla[CashPerSlot]);
            if (ShouldWrite(MaxBalance, s.MaxBalance)) SetStatic<CashInstance>("MAX_BALANCE", s.MaxBalance ?? _vanilla[MaxBalance]);
            if (ShouldWrite(AtmWeeklyLimit, s.AtmWeeklyLimit)) SetStatic<ATM>("WeeklyDepositLimit", s.AtmWeeklyLimit ?? _vanilla[AtmWeeklyLimit]);

            foreach (var rack in UnityEngine.Object.FindObjectsOfType<DryingRack>()) ApplyTo(rack, s);
            foreach (var mixer in UnityEngine.Object.FindObjectsOfType<MixingStation>()) ApplyTo(mixer, s);
        }

        const int FieldLiteral = 0x40;       // compile-time constant: no storage to write to
        const int ThreadStaticOffset = -1;   // per-thread storage: writing from here hits the wrong memory

        // Writes a static float through the raw il2cpp API after checking the field can be written;
        // a plain property set crashed the game natively when cash was changed.
        static void SetStatic<T>(string name, float value)
        {
            var cls = Il2CppClassPointerStore<T>.NativeClassPtr;
            IL2CPP.il2cpp_runtime_class_init(cls);
            var field = IL2CPP.il2cpp_class_get_field_from_name(cls, name);
            int flags = field == IntPtr.Zero ? 0 : (int)IL2CPP.il2cpp_field_get_flags(field);
            int offset = field == IntPtr.Zero ? 0 : (int)IL2CPP.il2cpp_field_get_offset(field);
            if (field == IntPtr.Zero || (flags & FieldLiteral) != 0 || offset == ThreadStaticOffset)
            {
                Core.ErrorOnce($"{typeof(T).Name}.{name} can't be changed in this game version (flags {flags:X}, offset {offset}).");
                return;
            }
            MelonLogger.Msg($"[cash] setting {typeof(T).Name}.{name} = {value} (flags {flags:X}, offset {offset})");
            unsafe { IL2CPP.il2cpp_field_static_set_value(field, &value); }
            MelonLogger.Msg($"[cash] set {typeof(T).Name}.{name}");
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
