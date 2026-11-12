using System.Collections.Generic;
using System.Linq;
using MDSAPI.Items;
using MDSAPI.Unity;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    // Group: the filter chip in the Items tab: a product's drug type (Weed, Cocaine, ...), otherwise its category
    public record CatalogItem(ItemDefinition Def, ItemInfo Info, string Name, Sprite? Icon, string Group);

    public class ItemCatalog
    {
        // vanilla StackLimit per item ID, captured before the mod first writes it; never overwritten
        static readonly Dictionary<string, int> VanillaLimits = new Dictionary<string, int>();

        readonly Dictionary<string, CatalogItem> _items = new Dictionary<string, CatalogItem>();
        static readonly HashSet<ObjectKey> _seen = new HashSet<ObjectKey>(); // cleared with the pointer cache on scene change

        public IReadOnlyList<CatalogItem> Items { get; private set; } = new List<CatalogItem>();

        public void Rescan()
        {
            foreach (var def in MDSAPI.Items.Items.AllDefinitions())
            {
                // already read this exact native object: skip the costly string reads (~50ms per menu open)
                if (!def.Exists || !_seen.Add(def.Key)) continue;
                var id = def.Id;
                if (string.IsNullOrEmpty(id)) continue;
                // re-add even if known: assets can be reloaded as new objects between scenes
                try
                {
                    var info = Info(def);
                    _items[id] = new CatalogItem(def, info, def.Name ?? id, def.Icon, GroupOf(def, info));
                }
                catch (System.Exception ex) { Core.ErrorOnce($"Could not read item {id}: {ex.Message}"); }
            }
            Items = _items.Values.Where(i => i.Def.Exists).OrderBy(i => i.Name).ToList();
        }

        // per native object, so the hot slot patch doesn't read ID/category strings on every call
        static readonly Dictionary<ObjectKey, ItemInfo> InfoByPointer = new Dictionary<ObjectKey, ItemInfo>();

        public static void ClearPointerCache() { InfoByPointer.Clear(); _seen.Clear(); }

        public static ItemInfo CachedInfo(ItemDefinition def) =>
            InfoByPointer.TryGetValue(def.Key, out var info) ? info : Track(def);

        // mixed products are created at runtime (on load, after our apply pass, or when mixing): limit them on first sight
        public static ItemInfo Track(ItemDefinition def)
        {
            var info = InfoByPointer[def.Key] = Info(def);
            def.StackLimit = LimitResolver.DefinitionLimit(Core.Settings, info, Core.Settings.Objects.Keys);
            return info;
        }

        public static ItemInfo Info(ItemDefinition def)
        {
            var id = def.Id;
            if (!VanillaLimits.TryGetValue(id, out var vanilla))
                VanillaLimits[id] = vanilla = MixSourceVanilla(def, id) ?? def.StackLimit;
            return new ItemInfo(id, def.Category ?? "", vanilla, vanilla <= 1 || def.HasOwnCount);
        }

        // A mix is a clone of its drug type's default product, so it copies that product's current (possibly
        // already modded) StackLimit. Its vanilla is the default's vanilla, not the copied value.
        static int? MixSourceVanilla(ItemDefinition def, string id)
        {
            if (!def.IsProduct) return null;
            var source = Products.DefaultFor(def.DrugType);
            if (!source.Exists) return null;
            var sourceId = source.Id;
            if (sourceId == id) return null;
            return VanillaLimits.TryGetValue(sourceId, out var v) ? v : source.StackLimit;
        }

        static string GroupOf(ItemDefinition def, ItemInfo info)
        {
            if (!def.IsProduct) return info.Category;
            // DrugType is null for templates the game never filled in (its own getter throws on those)
            switch (def.DrugType)
            {
                case null: return info.Category;
                case "Marijuana": return "Weed";
                case "Methamphetamine": return "Meth";
                case var other: return other;
            }
        }

        public bool TryGet(string id, out CatalogItem item) => _items.TryGetValue(id, out item!);

        public void Apply(ModSettings s)
        {
            Rescan(); // pick up products mixed since the last scan
            var objects = s.Objects.Keys.ToList();
            foreach (var item in Items)
            {
                if (!item.Def.Exists) continue;
                var def = item.Def;
                try { def.StackLimit = LimitResolver.DefinitionLimit(s, item.Info, objects); }
                catch (System.Exception ex) { Core.ErrorOnce($"Could not set stack limit for {item.Info.Id}: {ex.Message}"); }
            }
        }
    }
}
