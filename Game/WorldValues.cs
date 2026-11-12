using System.Collections.Generic;
using MDSAPI.Patching;
using MDSAPI.World;
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

        const float VanillaCashPerSlot = 1000f, VanillaAtmLimit = 10000f;

        // both are compiled-in consts (CashSlot.MAX_CASH_PER_SLOT, ATM.WeeklyDepositLimit), enforced only by these UI methods
        static CodeConstant? _cashPerSlot;
        static CodeConstant? _atmLimit;

        static readonly Dictionary<string, float> _vanilla = new Dictionary<string, float>
        {
            [CashPerSlot] = VanillaCashPerSlot,
            [AtmWeeklyLimit] = VanillaAtmLimit,
        };
        public static IReadOnlyDictionary<string, float> Vanilla => _vanilla;

        public static void Install()
        {
            _cashPerSlot = CodeConstant.Override("Cash per slot", "UI.Items.ItemUIManager", VanillaCashPerSlot,
                "StartDragCash", "EndCashDrag", "AddCashAmount", "SubtractCashAmount");
            _atmLimit = CodeConstant.Override("ATM weekly limit", "UI.ATMInterface", VanillaAtmLimit,
                "get_remainingAllowedDeposit", "Update", "SetSelectedAmount", "UpdateAvailableAmounts");
        }

        // cheap: no scene search
        public static void ApplyConstants(ModSettings s)
        {
            _cashPerSlot?.Set(s.CashPerSlot);
            LookTarget.InvalidateCash(); // a per-case value may be due again on the next frame
            _atmLimit?.Set(s.AtmWeeklyLimit);
        }

        // per-case override (LookTarget.TickCash); null = vanilla
        public static void SetCashPerSlot(float? value) => _cashPerSlot?.Set(value);

        public static void Apply(ModSettings s)
        {
            ApplyConstants(s);
            foreach (var rack in Stations.All(StationType.DryingRack)) ApplyRack(rack, s);
            foreach (var mixer in Stations.All(StationType.MixingStation)) ApplyMixer(mixer, s);
        }

        static void ApplyRack(Buildable rack, ModSettings s)
        {
            var current = rack.StationCapacity;
            if (current == null) return;
            Capture(DryingRack, current.Value);
            rack.StationCapacity = ObjectCapacity(s, rack.Guid) ?? s.DryingRackCapacity ?? (int)_vanilla[DryingRack];
        }

        static void ApplyMixer(Buildable mixer, ModSettings s)
        {
            var current = mixer.StationCapacity;
            if (current == null) return;
            var type = mixer.TypeName;
            var key = MixerKey(type);
            Capture(key, current.Value);
            mixer.StationCapacity = ObjectCapacity(s, mixer.Guid)
                ?? (s.MixerMaxMix.TryGetValue(type, out var v) ? v : (int)_vanilla[key]);
        }

        static int? ObjectCapacity(ModSettings s, string guid) => s.Objects.TryGetValue(guid, out var o) ? o.Capacity : null;

        static void Capture(string key, float value)
        {
            if (!_vanilla.ContainsKey(key)) _vanilla[key] = value;
        }
    }
}
