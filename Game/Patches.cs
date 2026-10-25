using System;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
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

    // InitializeGridItem runs when a rack/mixer is placed or loaded, after its GUID is set.
    [HarmonyPatch(typeof(DryingRack), nameof(DryingRack.InitializeGridItem))]
    static class DryingRackInitPatch
    {
        static void Postfix(DryingRack __instance)
        {
            try { WorldValues.ApplyTo(__instance, Core.Settings); }
            catch (Exception ex) { Core.ErrorOnce($"Drying rack patch failed: {ex.Message}"); }
        }
    }

    [HarmonyPatch(typeof(MixingStation), nameof(MixingStation.InitializeGridItem))]
    static class MixingStationInitPatch
    {
        static void Postfix(MixingStation __instance)
        {
            try { WorldValues.ApplyTo(__instance, Core.Settings); }
            catch (Exception ex) { Core.ErrorOnce($"Mixing station patch failed: {ex.Message}"); }
        }
    }
}
