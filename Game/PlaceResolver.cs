using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Delivery;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Storage;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    public static class PlaceResolver
    {
        static readonly Dictionary<IntPtr, (Place place, string? guid)> Cache = new();

        public static void ClearCache() => Cache.Clear();

        public static (Place place, string? guid) Resolve(ItemSlot slot)
        {
            if (Cache.TryGetValue(slot.Pointer, out var hit)) return hit;
            var result = ResolveOwner(slot.SlotOwner);
            Cache[slot.Pointer] = result;
            return result;
        }

        static (Place, string?) ResolveOwner(IItemSlotOwner? owner)
        {
            if (owner == null) return (Place.Pockets, null);

            // MixingStationMk2 / PackagingStationMk2 derive from the Mk1 types
            if (owner.TryCast<DryingRack>() is { } rack) return (Place.DryingRack, Guid(rack));
            if (owner.TryCast<BrickPress>() is { } press) return (Place.BrickPress, Guid(press));
            if (owner.TryCast<Cauldron>() is { } cauldron) return (Place.Cauldron, Guid(cauldron));
            if (owner.TryCast<ChemistryStation>() is { } chem) return (Place.ChemistryStation, Guid(chem));
            if (owner.TryCast<LabOven>() is { } oven) return (Place.LabOven, Guid(oven));
            if (owner.TryCast<MixingStation>() is { } mixer) return (Place.MixingStation, Guid(mixer));
            if (owner.TryCast<PackagingStation>() is { } pack) return (Place.PackagingStation, Guid(pack));
            if (owner.TryCast<NPCInventory>() != null || owner.TryCast<Dealer>() != null) return (Place.Npcs, null);

            if (owner.TryCast<StorageEntity>() is { } storage)
            {
                if (storage.GetComponentInParent<DeliveryVehicle>() != null) return (Place.Deliveries, null);
                if (storage.GetComponentInParent<Il2CppScheduleOne.Vehicles.LandVehicle>() is { } vehicle)
                    return (Place.Vehicles, ObjectSettings.VehicleKey(VehicleTypes.KeyOf(vehicle)));
                var buildable = storage.GetComponentInParent<BuildableItem>();
                return (Place.Storage, buildable != null ? Guid(buildable) : null);
            }

            return (Place.Pockets, null);
        }

        static string Guid(BuildableItem b) => b.GUID.ToString();
    }
}
