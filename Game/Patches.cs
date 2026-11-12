using System;
using MDSAPI.Items;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // The game hooks StackLimit Plus needs, installed through MDSAPI so a game update that moves
    // them is fixed in MDSAPI rather than here. Each returns false if this game version lacks it.
    public static class Hooks
    {
        public static void Install()
        {
            if (!Items.AddCapacityFilter(SlotCapacity))
                Core.ErrorOnce("Per-place and per-object limits are off: the slot capacity hook is unavailable in this game version.");
            Items.DefinitionRegistered += OnDefinitionRegistered;
            ItemDrag.BeforeDrop += ClampDrop;
        }

        // The definition's StackLimit is the max over all places; each slot is clamped to its own place here.
        // Hot path: the game calls this constantly (player, workers, every open inventory), so keep it allocation-free.
        static int SlotCapacity(ItemSlot slot, ItemInstance item, int capacity)
        {
            if (capacity <= 0 || item.Native == null) return capacity;
            try
            {
                var def = item.Definition;
                if (def.Native == null) return capacity;
                var (place, guid) = PlaceResolver.Resolve(slot);
                int limit = LimitResolver.SlotLimit(Core.Settings, ItemCatalog.CachedInfo(def), place, guid);
                return Math.Min(capacity, LimitResolver.Capacity(limit, slot.Quantity));
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Slot capacity patch failed: {ex.Message}");
                return capacity;
            }
        }

        // Every runtime product (mixes, on load and while playing, host and co-op) is registered right after it is
        // cloned. Limit it now: some game code (TryInsertItemIntoSet) reads StackLimit without asking a slot.
        static void OnDefinitionRegistered(ItemDefinition def)
        {
            try { if (def.Native != null) ItemCatalog.Track(def); }
            catch (Exception ex) { Core.ErrorOnce($"Could not limit new item: {ex.Message}"); }
        }

        // Dropping onto a matching stack, the game adds one at a time up to the item's StackLimit (the max over all places),
        // never asking the slot. Clamp the dragged amount to what the target slot takes, so per-place/object limits hold.
        static void ClampDrop(ItemDrop drop)
        {
            try
            {
                if (drop.IsCash) return;
                var source = drop.From;
                var target = drop.To;
                var item = source.Item;
                if (!target.Exists || !item.Exists || target.Key == source.Key) return;

                var existing = target.Item;
                if (existing.Exists && !existing.CanStackWith(item)) return; // a swap, not a stack
                int cap = target.CapacityFor(item);
                // an empty slot that refuses the item is the game's own call (filters, locks); leave it
                if (!existing.Exists && cap <= 0) return;
                if (drop.Amount > cap) drop.Amount = cap;
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Drag limit patch failed: {ex.Message}");
            }
        }
    }
}
