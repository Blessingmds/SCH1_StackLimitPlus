using System;
using System.Linq;

namespace SCH1_StackLimitPlus.Settings
{
    public enum PlacePreset { PocketsOnly, ExceptStations, Everywhere }

    public static class PlacePresets
    {
        static PlaceMode ModeFor(PlacePreset preset, Place p) => preset switch
        {
            PlacePreset.PocketsOnly => p == Place.Pockets ? PlaceMode.ItemLimits : PlaceMode.Vanilla,
            PlacePreset.ExceptStations => LimitResolver.IsStation(p) ? PlaceMode.Vanilla : PlaceMode.ItemLimits,
            _ => PlaceMode.ItemLimits,
        };

        // changes modes only; custom numbers stay so switching back to Custom restores them
        public static void Apply(ModSettings s, PlacePreset preset)
        {
            foreach (Place p in Enum.GetValues(typeof(Place)))
            {
                if (!s.Places.TryGetValue(p, out var ps)) s.Places[p] = ps = new PlaceSetting();
                ps.Mode = ModeFor(preset, p);
            }
        }

        public static PlacePreset? Current(ModSettings s)
        {
            foreach (PlacePreset preset in Enum.GetValues(typeof(PlacePreset)))
                if (Enum.GetValues(typeof(Place)).Cast<Place>().All(p => s.Places.TryGetValue(p, out var ps) && ps.Mode == ModeFor(preset, p)))
                    return preset;
            return null;
        }
    }
}
