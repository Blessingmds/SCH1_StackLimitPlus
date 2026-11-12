using System;
using MDSAPI.Players;
using MDSAPI.Unity;
using MDSAPI.World;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // CashCase: a worker's bed; its case only holds cash, so it gets a cash-per-slot setting instead of stack sizes
    public record Target(Buildable Item, string Guid, string Name, Place Place, bool HasCapacity, bool CashCase = false);

    // The placed rack/station/storage the player is aiming at: whatever the game itself is showing its
    // "[E] Use ..." prompt for, so our "[F6] Change limit" appears on exactly the same parts of the object.
    public static class LookTarget
    {
        static ObjectKey _lastItem;

        public static Target? Current { get; private set; }

        public static void Tick(bool menuOpen)
        {
            if (menuOpen) { Current = null; _lastItem = ObjectKey.None; return; }
            try
            {
                var item = PlayerControls.OpenScreens > 0 ? default : Interaction.HoveredBuildable;
                if (!item.Exists) { Current = null; _lastItem = ObjectKey.None; return; }
                if (item.Key == _lastItem) return; // same object as last frame: keep the cached description
                _lastItem = item.Key;
                Current = Describe(item);
            }
            catch (Exception ex)
            {
                Current = null;
                _lastItem = ObjectKey.None;
                Core.ErrorOnce($"Look target failed: {ex.Message}");
            }
        }

        static Target? Describe(Buildable item)
        {
            // beds hold no items; a worker's bed has a case for their cash, so only that gets a (cash) page
            if (item.IsBed) // a component on the bed object, not a BuildableItem subtype
                return item.IsEmployeeBed ? new Target(item, item.Guid, item.Name, Place.Storage, false, CashCase: true) : null;
            Place? place = null;
            bool capacity = false;
            switch (item.Station)
            {
                case StationType.DryingRack: place = Place.DryingRack; capacity = true; break;
                case StationType.MixingStation: place = Place.MixingStation; capacity = true; break;
                case StationType.BrickPress: place = Place.BrickPress; break;
                case StationType.Cauldron: place = Place.Cauldron; break;
                case StationType.ChemistryStation: place = Place.ChemistryStation; break;
                case StationType.LabOven: place = Place.LabOven; break;
                case StationType.PackagingStation: place = Place.PackagingStation; break;
                default: if (item.Storage.Exists) place = Place.Storage; break;
            }
            if (place == null) return null;
            return new Target(item, item.Guid, item.Name, place.Value, capacity);
        }

        // Per-case cash: the game's cash limit is one number, so swap in the case's own value while that case is
        // open (drag and shift-click both move cash between it and the pockets), and the global value otherwise.
        static float? _appliedCash = float.NaN;

        public static void TickCash()
        {
            float? want = Core.Settings.CashPerSlot;
            try
            {
                if (Core.Settings.Objects.Count > 0)
                {
                    var storage = Interaction.OpenStorage;
                    if (storage.Exists) want = CaseCash(storage.FirstSlot) ?? want;
                }
            }
            catch (Exception ex) { Core.ErrorOnce($"Per-case cash failed: {ex.Message}"); }
            if (want == _appliedCash) return;
            _appliedCash = want;
            WorldValues.SetCashPerSlot(want);
        }

        // the global value changed (settings): re-apply on the next tick
        public static void InvalidateCash() => _appliedCash = float.NaN;

        static float? CaseCash(MDSAPI.Items.ItemSlot slot)
        {
            if (!slot.Exists) return null;
            var (_, guid) = PlaceResolver.Resolve(slot);
            return guid != null && Core.Settings.Objects.TryGetValue(guid, out var o) ? o.CashPerSlot : null;
        }
    }
}
