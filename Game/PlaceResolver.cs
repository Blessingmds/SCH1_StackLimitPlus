using System.Collections.Generic;
using MDSAPI.Items;
using MDSAPI.Unity;
using MDSAPI.World;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    public static class PlaceResolver
    {
        static readonly Dictionary<ObjectKey, (Place place, string? guid)> Cache = new Dictionary<ObjectKey, (Place, string?)>();

        public static void ClearCache() => Cache.Clear();

        public static (Place place, string? guid) Resolve(ItemSlot slot)
        {
            var key = slot.Key;
            if (Cache.TryGetValue(key, out var hit)) return hit;
            var result = ResolveOwner(slot.Owner);
            Cache[key] = result;
            return result;
        }

        static (Place, string?) ResolveOwner(SlotOwner owner)
        {
            if (!owner.Exists) return (Place.Pockets, null);

            // upgraded marks (MixingStationMk2, PackagingStationMk2) count as their first mark
            switch (owner.Station)
            {
                case StationType.DryingRack: return (Place.DryingRack, owner.Buildable.Guid);
                case StationType.BrickPress: return (Place.BrickPress, owner.Buildable.Guid);
                case StationType.Cauldron: return (Place.Cauldron, owner.Buildable.Guid);
                case StationType.ChemistryStation: return (Place.ChemistryStation, owner.Buildable.Guid);
                case StationType.LabOven: return (Place.LabOven, owner.Buildable.Guid);
                case StationType.MixingStation: return (Place.MixingStation, owner.Buildable.Guid);
                case StationType.PackagingStation: return (Place.PackagingStation, owner.Buildable.Guid);
            }
            if (owner.IsNpc) return (Place.Npcs, null);

            var storage = owner.Storage;
            if (storage.Exists)
            {
                if (storage.IsDeliveryVehicle) return (Place.Deliveries, null);
                var vehicle = storage.Vehicle;
                if (vehicle.Exists) return (Place.Vehicles, ObjectSettings.VehicleKey(VehicleTypes.KeyOf(vehicle)));
                var buildable = storage.Buildable;
                return (Place.Storage, buildable.Exists ? buildable.Guid : null);
            }

            return (Place.Pockets, null);
        }
    }
}
