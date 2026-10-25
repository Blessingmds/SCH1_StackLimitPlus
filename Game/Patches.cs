using System;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // The definition's StackLimit is the max over all places; each slot is clamped to its own place here.
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
                int limit = LimitResolver.SlotLimit(Core.Settings, ItemCatalog.Info(def), place, guid);
                __result = Math.Min(__result, LimitResolver.Capacity(limit, __instance.Quantity));
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Slot capacity patch failed: {ex.Message}");
            }
        }
    }
}
