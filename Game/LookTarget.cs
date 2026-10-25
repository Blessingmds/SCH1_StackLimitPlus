using System;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Storage;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    public record Target(BuildableItem Item, string Guid, string Name, Place Place, bool HasCapacity);

    // The placed rack/station/storage the player is aiming at, using the game's own interaction ray.
    public static class LookTarget
    {
        const float Range = 4f;
        static int _frame;

        public static Target? Current { get; private set; }

        public static void Tick(bool menuOpen)
        {
            if (++_frame % 5 != 0) return;
            Current = null;
            if (menuOpen) return;
            try
            {
                var cam = PlayerSingleton<PlayerCamera>.Instance;
                if (cam == null || cam.ActiveUIElementCount > 0) return;
                if (!cam.LookRaycast(Range, out RaycastHit hit, (LayerMask)(-1), false, 0f) || hit.collider == null) return;
                var item = hit.collider.GetComponentInParent<BuildableItem>();
                if (item == null) return;
                Current = Describe(item);
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Look target failed: {ex.Message}");
            }
        }

        static Target? Describe(BuildableItem item)
        {
            Place? place = null;
            bool capacity = false;
            if (item.TryCast<DryingRack>() != null) { place = Place.DryingRack; capacity = true; }
            else if (item.TryCast<MixingStation>() != null) { place = Place.MixingStation; capacity = true; }
            else if (item.TryCast<BrickPress>() != null) place = Place.BrickPress;
            else if (item.TryCast<Cauldron>() != null) place = Place.Cauldron;
            else if (item.TryCast<ChemistryStation>() != null) place = Place.ChemistryStation;
            else if (item.TryCast<LabOven>() != null) place = Place.LabOven;
            else if (item.TryCast<PackagingStation>() != null) place = Place.PackagingStation;
            else if (item.GetComponentInChildren<StorageEntity>() != null) place = Place.Storage;
            if (place == null) return null;

            var name = item.ItemInstance?.Name;
            if (string.IsNullOrEmpty(name)) name = item.gameObject.name.Replace("(Clone)", "").Trim();
            return new Target(item, item.GUID.ToString(), name!, place.Value, capacity);
        }
    }
}
