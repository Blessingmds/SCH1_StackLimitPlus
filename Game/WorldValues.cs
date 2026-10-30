using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ObjectScripts;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // ATM limit, drying rack capacity and mixer max mix. Vanilla values are captured before the first write.
    public static class WorldValues
    {
        public const string CashPerSlot = "CashPerSlot";
        public const string AtmWeeklyLimit = "AtmWeeklyLimit";
        public const string DryingRack = "DryingRack";
        public static string MixerKey(string typeName) => "Mixer:" + typeName;

        static readonly Dictionary<string, float> _vanilla = new() { [CashPerSlot] = CashSlotCap.Vanilla };
        public static IReadOnlyDictionary<string, float> Vanilla => _vanilla;
        static bool? _atmLimitWritten;

        public static void Apply(ModSettings s)
        {
            TickAtm(s);
            foreach (var rack in UnityEngine.Object.FindObjectsOfType<DryingRack>()) ApplyTo(rack, s);
            foreach (var mixer in UnityEngine.Object.FindObjectsOfType<MixingStation>()) ApplyTo(mixer, s);
        }

        // ATM.WeeklyDepositLimit is a compiled-in constant (writing it crashed the game); its on/off switch is a real field.
        // Called every second: switch the game's limit off until this week's deposits reach the custom limit.
        public static void TickAtm(ModSettings s)
        {
            IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<ATM>.NativeClassPtr);
            Capture(AtmWeeklyLimit, ATM.WeeklyDepositLimit);
            bool enabled = AtmLimit.GameLimitEnabled(s.AtmWeeklyLimit, _vanilla[AtmWeeklyLimit], ATM.WeeklyDepositSum);
            // leave the game's own value alone until we need to change it
            if (_atmLimitWritten == null && enabled) return;
            if (_atmLimitWritten == enabled) return;
            if (SetStatic<ATM, bool>("DepositLimitEnabled", enabled)) _atmLimitWritten = enabled;
        }

        const int FieldLiteral = 0x40;       // compile-time constant: no storage to write to
        const int ThreadStaticOffset = -1;   // per-thread storage: writing from here hits the wrong memory

        // Writes a static through the raw il2cpp API after checking the field has storage; constants get an error instead of a crash.
        static unsafe bool SetStatic<TClass, TValue>(string name, TValue value) where TValue : unmanaged
        {
            var cls = Il2CppClassPointerStore<TClass>.NativeClassPtr;
            IL2CPP.il2cpp_runtime_class_init(cls);
            var field = IL2CPP.il2cpp_class_get_field_from_name(cls, name);
            int flags = field == IntPtr.Zero ? 0 : (int)IL2CPP.il2cpp_field_get_flags(field);
            int offset = field == IntPtr.Zero ? 0 : (int)IL2CPP.il2cpp_field_get_offset(field);
            if (field == IntPtr.Zero || (flags & FieldLiteral) != 0 || offset == ThreadStaticOffset)
            {
                Core.ErrorOnce($"{typeof(TClass).Name}.{name} can't be changed in this game version (flags {flags:X}, offset {offset}).");
                return false;
            }
            IL2CPP.il2cpp_field_static_set_value(field, &value);
            return true;
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
