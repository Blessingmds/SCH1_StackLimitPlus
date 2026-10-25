using System;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using MelonLoader;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // The definition's StackLimit is the max over all places; each slot is clamped to its own place here.
    [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetCapacityForItem))]
    static class ItemSlotCapacityPatch
    {
        static bool _logged;

        static void Postfix(ItemSlot __instance, ItemInstance item, ref int __result)
        {
            // shortcut: first-call trace to locate a native crash on save load; remove once 2.0 is verified in game
            if (!_logged) MelonLogger.Msg("[trace] slot capacity patch: first call");
            if (__result <= 0 || item == null) return;
            try
            {
                var def = item.Definition;
                if (def == null) return;
                var (place, guid) = PlaceResolver.Resolve(__instance);
                int limit = LimitResolver.SlotLimit(Core.Settings, ItemCatalog.Info(def), place, guid);
                __result = Math.Min(__result, LimitResolver.Capacity(limit, __instance.Quantity));
                if (!_logged) { _logged = true; MelonLogger.Msg($"[trace] slot capacity patch ok: {place}"); }
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Slot capacity patch failed: {ex.Message}");
            }
        }
    }
}
