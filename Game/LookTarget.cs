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
    // CashCase: a worker's bed; its case only holds cash, so it gets a cash-per-slot setting instead of stack sizes
    public record Target(BuildableItem Item, string Guid, string Name, Place Place, bool HasCapacity, bool CashCase = false);

    // The placed rack/station/storage the player is aiming at: whatever the game itself is showing its
    // "[E] Use ..." prompt for, so our "[F6] Change limit" appears on exactly the same parts of the object.
    public static class LookTarget
    {
        static IntPtr _lastItem;

        public static Target? Current { get; private set; }

        public static void Tick(bool menuOpen)
        {
            if (menuOpen) { Current = null; _lastItem = IntPtr.Zero; return; }
            try
            {
                var cam = PlayerSingleton<PlayerCamera>.Instance;
                var hovered = Singleton<Il2CppScheduleOne.Interaction.InteractionManager>.Instance?.HoveredValidInteractableObject;
                var item = cam == null || cam.ActiveUIElementCount > 0 || hovered == null ? null : hovered.GetComponentInParent<BuildableItem>();
                if (item == null) { Current = null; _lastItem = IntPtr.Zero; return; }
                if (item.Pointer == _lastItem) return; // same object as last frame: keep the cached description
                _lastItem = item.Pointer;
                Current = Describe(item);
            }
            catch (Exception ex)
            {
                Current = null;
                _lastItem = IntPtr.Zero;
                Core.ErrorOnce($"Look target failed: {ex.Message}");
            }
        }

        static Target? Describe(BuildableItem item)
        {
            // beds hold no items; a worker's bed has a case for their cash, so only that gets a (cash) page
            if (item.GetComponentInChildren<Bed>() is { } bed) // a component on the bed object, not a BuildableItem subtype
                return bed.AssignedEmployee != null ? new Target(item, item.GUID.ToString(), NameOf(item), Place.Storage, false, CashCase: true) : null;
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
            return new Target(item, item.GUID.ToString(), NameOf(item), place.Value, capacity);
        }

        static string NameOf(BuildableItem item)
        {
            var name = item.ItemInstance?.Name;
            return string.IsNullOrEmpty(name) ? item.gameObject.name.Replace("(Clone)", "").Trim() : name!;
        }

        // Per-case cash: the game's cash limit is one number, so swap in the case's own value while that case is
        // open (drag and shift-click both move cash between it and the pockets), and the global value otherwise.
        static float? _appliedCash = float.NaN;

        public static void TickCash()
        {
            float? want = Core.Settings.CashPerSlot;
            try
            {
                var menu = Singleton<Il2CppScheduleOne.UI.StorageMenu>.Instance;
                var slots = menu != null && menu.IsOpen ? menu.OpenedStorageEntity?.ItemSlots : null;
                if (slots != null && slots.Count > 0 && Core.Settings.Objects.Count > 0)
                    want = CaseCash(slots[0]) ?? want;
            }
            catch (Exception ex) { Core.ErrorOnce($"Per-case cash failed: {ex.Message}"); }
            if (want == _appliedCash) return;
            _appliedCash = want;
            WorldValues.SetCashPerSlot(want);
        }

        // the global value changed (settings): re-apply on the next tick
        public static void InvalidateCash() => _appliedCash = float.NaN;

        static float? CaseCash(Il2CppScheduleOne.ItemFramework.ItemSlot? slot)
        {
            if (slot == null) return null;
            var (_, guid) = PlaceResolver.Resolve(slot);
            return guid != null && Core.Settings.Objects.TryGetValue(guid, out var o) ? o.CashPerSlot : null;
        }
    }
}
