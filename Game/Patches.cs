using System;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // The definition's StackLimit is the max over all places; each slot is clamped to its own place here.
    // Hot path: the game calls this constantly (player, workers, every open inventory), so keep it allocation-free.
    [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetCapacityForItem))]
    static class ItemSlotCapacityPatch
    {
        static void Postfix(ItemSlot __instance, ItemInstance item, ref int __result)
        {
            if (__result <= 0 || item == null) return;
            try
            {
                var def = item.Definition;
                if (def == null) return;
                var (place, guid) = PlaceResolver.Resolve(__instance);
                int limit = LimitResolver.SlotLimit(Core.Settings, ItemCatalog.CachedInfo(def), place, guid);
                __result = Math.Min(__result, LimitResolver.Capacity(limit, __instance.Quantity));
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Slot capacity patch failed: {ex.Message}");
            }
        }
    }
}

namespace SCH1_StackLimitPlus.Game
{
    // Dropping onto a matching stack, the game adds one at a time up to the item's StackLimit (the max over all places),
    // never asking the slot. Clamp the dragged amount to what the target slot takes, so per-place/object limits hold.
    [HarmonyPatch(typeof(Il2CppScheduleOne.UI.Items.ItemUIManager), nameof(Il2CppScheduleOne.UI.Items.ItemUIManager.EndDrag))]
    static class DragDropClampPatch
    {
        static void Prefix(Il2CppScheduleOne.UI.Items.ItemUIManager __instance)
        {
            try
            {
                if (__instance.isDraggingCash) return;
                var source = __instance.draggedSlot?.assignedSlot;
                var target = __instance.HoveredSlot?.assignedSlot;
                var item = source?.ItemInstance;
                if (target == null || item == null || target.Pointer == source!.Pointer) return;

                var existing = target.ItemInstance;
                if (existing != null && !existing.CanStackWith(item, false)) return; // a swap, not a stack
                int cap = target.GetCapacityForItem(item, false);
                // an empty slot that refuses the item is the game's own call (filters, locks); leave it
                if (existing == null && cap <= 0) return;
                if (__instance.draggedAmount > cap) __instance.draggedAmount = cap;
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Drag limit patch failed: {ex.Message}");
            }
        }
    }
}
