using System;
using System.Collections.Generic;

namespace SCH1_StackLimitPlus.Settings
{
    public static class LimitResolver
    {
        public static readonly Place[] Stations =
        {
            Place.DryingRack, Place.BrickPress, Place.Cauldron, Place.ChemistryStation,
            Place.LabOven, Place.MixingStation, Place.PackagingStation
        };

        public static bool IsStation(Place p) => Array.IndexOf(Stations, p) >= 0;

        public static int ItemLimit(ModSettings s, ItemInfo i)
        {
            if (s.Items.TryGetValue(i.Id, out var item)) return item;
            if (i.Excluded && !s.IncludeUnstackable) return i.Vanilla;
            if (s.Categories.TryGetValue(i.Category, out var cat)) return cat;
            return s.Global ?? i.Vanilla;
        }

        public static int SlotLimit(ModSettings s, ItemInfo i, Place p, string? objectGuid)
        {
            if (objectGuid != null && s.Objects.TryGetValue(objectGuid, out var o) && o.SlotMode != ObjectSlotMode.FollowPlace)
                return Resolve(s, i, (PlaceMode)(o.SlotMode - 1), o.SlotCustom); // ObjectSlotMode = FollowPlace + PlaceMode, same order

            var place = s.Places.TryGetValue(p, out var ps) ? ps : new PlaceSetting();
            return Resolve(s, i, place.Mode, place.Custom);
        }

        static int Resolve(ModSettings s, ItemInfo i, PlaceMode mode, int custom) => mode switch
        {
            PlaceMode.Vanilla => i.Vanilla,
            // excluded items only leave vanilla through an explicit per-item value
            PlaceMode.Custom => i.Excluded && !s.IncludeUnstackable && !s.Items.ContainsKey(i.Id) ? i.Vanilla : custom,
            _ => ItemLimit(s, i),
        };

        public static int DefinitionLimit(ModSettings s, ItemInfo i, IEnumerable<string> objectGuids)
        {
            int max = i.Vanilla;
            foreach (Place p in Enum.GetValues(typeof(Place)))
                max = Math.Max(max, SlotLimit(s, i, p, null));
            foreach (var g in objectGuids)
                max = Math.Max(max, SlotLimit(s, i, Place.Pockets, g));
            return max;
        }

        public static int Capacity(int slotLimit, int quantity) => Math.Max(0, slotLimit - quantity);
    }
}
