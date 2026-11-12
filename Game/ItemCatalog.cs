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
                try
                {
                    var info = Info(def);
                    _items[def.ID] = new CatalogItem(def, info, def.Name ?? def.ID, def.Icon, GroupOf(def, info));
                }
                catch (System.Exception ex) { Core.ErrorOnce($"Could not read item {def.ID}: {ex.Message}"); }
            }
            Items = _items.Values.Where(i => i.Def != null).OrderBy(i => i.Name).ToList();
        }

        // per native object, so the hot slot patch doesn't marshal ID/category strings on every call
        static readonly Dictionary<System.IntPtr, ItemInfo> InfoByPointer = new();

        public static void ClearPointerCache() { InfoByPointer.Clear(); _seen.Clear(); }

        public static ItemInfo CachedInfo(BaseItemDefinition def) =>
            InfoByPointer.TryGetValue(def.Pointer, out var info) ? info : Track(def);

        // mixed products are created at runtime (on load, after our apply pass, or when mixing): limit them on first sight
        public static ItemInfo Track(BaseItemDefinition def)
        {
            var info = InfoByPointer[def.Pointer] = Info(def);
            def.StackLimit = LimitResolver.DefinitionLimit(Core.Settings, info, Core.Settings.Objects.Keys);
            return info;
        }

        public static ItemInfo Info(BaseItemDefinition def)
        {
            if (!VanillaLimits.TryGetValue(def.ID, out var vanilla))
                VanillaLimits[def.ID] = vanilla = MixSourceVanilla(def) ?? def.StackLimit;
            bool hasOwnCount = def.TryCast<IntegerItemDefinition>() != null;
            return new ItemInfo(def.ID, def.Category.ToString(), vanilla, vanilla <= 1 || hasOwnCount);
        }

        // A mix is a clone of its drug type's default product, so it copies that product's current (possibly
        // already modded) StackLimit. Its vanilla is the default's vanilla, not the copied value.
        static int? MixSourceVanilla(BaseItemDefinition def)
        {
            var product = def.TryCast<Il2CppScheduleOne.Product.ProductDefinition>();
            var pm = Il2CppScheduleOne.DevUtilities.NetworkSingleton<Il2CppScheduleOne.Product.ProductManager>.Instance;
            if (product == null || pm == null) return null;
            Il2CppScheduleOne.Product.ProductDefinition? source = DrugTypeOf(product) switch
            {
                "Marijuana" => pm.DefaultWeed,
                "Methamphetamine" => pm.DefaultMeth,
                "Cocaine" => pm.DefaultCocaine,
                "Shrooms" => pm.DefaultShroom,
                _ => null,
            };
            if (source == null || source.ID == def.ID) return null;
            return VanillaLimits.TryGetValue(source.ID, out var v) ? v : source.StackLimit;
        }

        static string GroupOf(ItemDefinition def, ItemInfo info)
        {
            var product = def.TryCast<Il2CppScheduleOne.Product.ProductDefinition>();
            if (product == null) return info.Category;
            return DrugTypeOf(product) switch
            {
                null => info.Category,
                "Marijuana" => "Weed",
                "Methamphetamine" => "Meth",
                var other => other,
            };
        }

        // the game's DrugType getter reads DrugTypes[0] and throws on templates that were never initialized
        static string? DrugTypeOf(Il2CppScheduleOne.Product.ProductDefinition p) =>
            p.DrugTypes != null && p.DrugTypes.Count > 0 ? p.DrugTypes[0].DrugType.ToString() : null;

        public bool TryGet(string id, out CatalogItem item) => _items.TryGetValue(id, out item!);

        public void Apply(ModSettings s)
        {
            Rescan(); // pick up products mixed since the last scan
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
