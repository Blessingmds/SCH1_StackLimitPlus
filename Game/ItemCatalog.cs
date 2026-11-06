using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.ItemFramework;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    // Group: the filter chip in the Items tab: a product's drug type (Weed, Cocaine, ...), otherwise its category
    public record CatalogItem(ItemDefinition Def, ItemInfo Info, string Name, Sprite? Icon, string Group);

    public class ItemCatalog
    {
        // vanilla StackLimit per item ID, captured before the mod first writes it; never overwritten
        static readonly Dictionary<string, int> VanillaLimits = new();

        readonly Dictionary<string, CatalogItem> _items = new();
        static readonly HashSet<System.IntPtr> _seen = new(); // cleared with the pointer cache on scene change

        public IReadOnlyList<CatalogItem> Items { get; private set; } = new List<CatalogItem>();

        public void Rescan()
        {
            foreach (var def in Resources.FindObjectsOfTypeAll<ItemDefinition>())
            {
                // already read this exact native object: skip the costly string marshalling (~50ms per menu open)
                if (def == null || !_seen.Add(def.Pointer)) continue;
                if (string.IsNullOrEmpty(def.ID)) continue;
                // re-add even if known: assets can be reloaded as new objects between scenes
                var info = Info(def);
                _items[def.ID] = new CatalogItem(def, info, def.Name ?? def.ID, def.Icon, GroupOf(def, info));
            }
            Items = _items.Values.Where(i => i.Def != null).OrderBy(i => i.Name).ToList();
        }

        // per native object, so the hot slot patch doesn't marshal ID/category strings on every call
        static readonly Dictionary<System.IntPtr, ItemInfo> InfoByPointer = new();

        public static void ClearPointerCache() { InfoByPointer.Clear(); _seen.Clear(); }

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

        static string GroupOf(ItemDefinition def, ItemInfo info)
        {
            var product = def.TryCast<Il2CppScheduleOne.Product.ProductDefinition>();
            if (product == null) return info.Category;
            return product.DrugType.ToString() switch
            {
                "Marijuana" => "Weed",
                "Methamphetamine" => "Meth",
                var other => other,
            };
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
