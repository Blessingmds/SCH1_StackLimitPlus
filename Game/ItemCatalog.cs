using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.ItemFramework;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    public record CatalogItem(ItemDefinition Def, ItemInfo Info, string Name, Sprite? Icon);

    public class ItemCatalog
    {
        // vanilla StackLimit per item ID, captured before the mod first writes it; never overwritten
        static readonly Dictionary<string, int> VanillaLimits = new();

        readonly Dictionary<string, CatalogItem> _items = new();

        public IReadOnlyList<CatalogItem> Items { get; private set; } = new List<CatalogItem>();

        public void Rescan()
        {
            foreach (var def in Resources.FindObjectsOfTypeAll<ItemDefinition>())
            {
                if (def == null || string.IsNullOrEmpty(def.ID)) continue;
                // re-add even if known: assets can be reloaded as new objects between scenes
                _items[def.ID] = new CatalogItem(def, Info(def), def.Name ?? def.ID, def.Icon);
            }
            Items = _items.Values.Where(i => i.Def != null).OrderBy(i => i.Name).ToList();
        }

        // per native object, so the hot slot patch doesn't marshal ID/category strings on every call
        static readonly Dictionary<System.IntPtr, ItemInfo> InfoByPointer = new();

        public static void ClearPointerCache() => InfoByPointer.Clear();

        public static ItemInfo CachedInfo(BaseItemDefinition def)
        {
            if (!InfoByPointer.TryGetValue(def.Pointer, out var info))
                InfoByPointer[def.Pointer] = info = Info(def);
            return info;
        }

        public static ItemInfo Info(BaseItemDefinition def)
        {
            if (!VanillaLimits.TryGetValue(def.ID, out var vanilla))
                VanillaLimits[def.ID] = vanilla = def.StackLimit;
            bool hasOwnCount = def.TryCast<IntegerItemDefinition>() != null;
            return new ItemInfo(def.ID, def.Category.ToString(), vanilla, vanilla <= 1 || hasOwnCount);
        }

        public bool TryGet(string id, out CatalogItem item) => _items.TryGetValue(id, out item!);

        public void Apply(ModSettings s)
        {
            var objects = s.Objects.Keys.ToList();
            foreach (var item in Items)
            {
                if (item.Def == null) continue;
                try { item.Def.StackLimit = LimitResolver.DefinitionLimit(s, item.Info, objects); }
                catch (System.Exception ex) { Core.ErrorOnce($"Could not set stack limit for {item.Info.Id}: {ex.Message}"); }
            }
        }
    }
}
