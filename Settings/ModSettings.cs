using System.Collections.Generic;

namespace SCH1_StackLimitPlus.Settings
{
    public enum Place { Pockets, Storage, Deliveries, Npcs, DryingRack, BrickPress, Cauldron, ChemistryStation, LabOven, MixingStation, PackagingStation, Vehicles }

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
        // a worker's bed case: cash per slot for this one case, over the global CashPerSlot
        public float? CashPerSlot { get; set; }
    }

    // Excluded = vanilla stack of 1 or an internal per-unit count (magazines); global/category limits skip it.
    // A plain struct, not a record struct: record structs are C# 10, and the Mono build is C# 9.
    public readonly struct ItemInfo : System.IEquatable<ItemInfo>
    {
        public string Id { get; }
        public string Category { get; }
        public int Vanilla { get; }
        public bool Excluded { get; }

        public ItemInfo(string Id, string Category, int Vanilla, bool Excluded)
        {
            this.Id = Id; this.Category = Category; this.Vanilla = Vanilla; this.Excluded = Excluded;
        }

        public void Deconstruct(out string id, out string category, out int vanilla, out bool excluded)
        {
            id = Id; category = Category; vanilla = Vanilla; excluded = Excluded;
        }

        public bool Equals(ItemInfo o) => Id == o.Id && Category == o.Category && Vanilla == o.Vanilla && Excluded == o.Excluded;
        public override bool Equals(object? obj) => obj is ItemInfo o && Equals(o);
        public override int GetHashCode() => System.HashCode.Combine(Id, Category, Vanilla, Excluded);
        public override string ToString() => $"ItemInfo {{ Id = {Id}, Category = {Category}, Vanilla = {Vanilla}, Excluded = {Excluded} }}";
    }

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
        // personal, like Hotkey: set when the player finishes or skips the walkthrough; missing in older files = false
        public bool HasTutorialCompleted { get; set; }
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
