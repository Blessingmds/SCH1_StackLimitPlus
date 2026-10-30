using System.Collections.Generic;

namespace SCH1_StackLimitPlus.Settings
{
    public enum Place { Pockets, Storage, Deliveries, Npcs, DryingRack, BrickPress, Cauldron, ChemistryStation, LabOven, MixingStation, PackagingStation }

    public enum PlaceMode { ItemLimits, Vanilla, Custom }

    public enum ObjectSlotMode { FollowPlace, ItemLimits, Vanilla, Custom }

    public class PlaceSetting
    {
        public PlaceMode Mode { get; set; }
        public int Custom { get; set; } = 100;
    }

    public class ObjectSetting
    {
        public ObjectSlotMode SlotMode { get; set; }
        public int SlotCustom { get; set; } = 100;
        public int? Capacity { get; set; }
    }

    // Excluded = vanilla stack of 1 or an internal per-unit count (magazines); global/category limits skip it.
    public readonly record struct ItemInfo(string Id, string Category, int Vanilla, bool Excluded);

    public class ModSettings
    {
        public int? Global { get; set; }
        public Dictionary<string, int> Categories { get; set; } = new();
        public Dictionary<string, int> Items { get; set; } = new();
        public bool IncludeUnstackable { get; set; }
        public Dictionary<Place, PlaceSetting> Places { get; set; } = new();
        public float? CashPerSlot { get; set; }
        public float? AtmWeeklyLimit { get; set; }
        public int? DryingRackCapacity { get; set; }
        public Dictionary<string, int> MixerMaxMix { get; set; } = new();
        public Dictionary<string, ObjectSetting> Objects { get; set; } = new();
        public string Hotkey { get; set; } = "F6";
        // F6 window size in reference pixels (1080p); personal, never synced
        public int? WindowWidth { get; set; }
        public int? WindowHeight { get; set; }

        public static Dictionary<Place, PlaceSetting> DefaultPlaces()
        {
            var places = new Dictionary<Place, PlaceSetting>();
            foreach (Place p in System.Enum.GetValues(typeof(Place)))
                places[p] = new PlaceSetting { Mode = LimitResolver.IsStation(p) ? PlaceMode.Vanilla : PlaceMode.ItemLimits };
            return places;
        }

        public static ModSettings CreateDefault() => new() { Places = DefaultPlaces() };
    }
}
