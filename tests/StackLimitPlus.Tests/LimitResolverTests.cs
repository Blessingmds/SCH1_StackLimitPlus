using SCH1_StackLimitPlus.Settings;
using Xunit;

public class LimitResolverTests
{
    static ItemInfo Kush => new("ogkush", "Product", 20, false);

    [Fact]
    public void ItemOverrideBeatsCategoryBeatsGlobal()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 100;
        s.Categories["Product"] = 50;
        s.Items["ogkush"] = 200;
        Assert.Equal(200, LimitResolver.ItemLimit(s, Kush));
        s.Items.Remove("ogkush");
        Assert.Equal(50, LimitResolver.ItemLimit(s, Kush));
        s.Categories.Remove("Product");
        Assert.Equal(100, LimitResolver.ItemLimit(s, Kush));
    }

    [Fact]
    public void AllUnsetIsVanilla()
    {
        var s = ModSettings.CreateDefault();
        Assert.Equal(20, LimitResolver.ItemLimit(s, Kush));
        Assert.Equal(20, LimitResolver.SlotLimit(s, Kush, Place.Pockets, null));
    }

    [Fact]
    public void ExcludedItemIgnoresGlobalAndCategory()
    {
        var mag = new ItemInfo("m1911mag", "Equipment", 1, true);
        var s = ModSettings.CreateDefault();
        s.Global = 99;
        s.Categories["Equipment"] = 50;
        Assert.Equal(1, LimitResolver.ItemLimit(s, mag));
        s.Items["m1911mag"] = 5;
        Assert.Equal(5, LimitResolver.ItemLimit(s, mag));
        s.Items.Remove("m1911mag");
        s.IncludeUnstackable = true;
        Assert.Equal(50, LimitResolver.ItemLimit(s, mag));
    }

    [Fact]
    public void ExcludedItemIgnoresCustomPlace()
    {
        var mag = new ItemInfo("m1911mag", "Equipment", 1, true);
        var s = ModSettings.CreateDefault();
        s.Places[Place.Storage] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 300 };
        Assert.Equal(1, LimitResolver.SlotLimit(s, mag, Place.Storage, null));
    }

    [Fact]
    public void StationsDefaultVanilla()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 999;
        Assert.Equal(20, LimitResolver.SlotLimit(s, Kush, Place.DryingRack, null));
        Assert.Equal(999, LimitResolver.SlotLimit(s, Kush, Place.Pockets, null));
    }

    [Fact]
    public void CustomPlace()
    {
        var s = ModSettings.CreateDefault();
        s.Places[Place.Deliveries] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 500 };
        Assert.Equal(500, LimitResolver.SlotLimit(s, Kush, Place.Deliveries, null));
        Assert.Equal(20, LimitResolver.SlotLimit(s, Kush, Place.Pockets, null));
    }

    [Fact]
    public void ObjectOverridesPlace()
    {
        var s = ModSettings.CreateDefault();
        s.Objects["g1"] = new ObjectSetting { SlotMode = ObjectSlotMode.Custom, SlotCustom = 40 };
        Assert.Equal(40, LimitResolver.SlotLimit(s, Kush, Place.DryingRack, "g1"));
        s.Objects["g1"].SlotMode = ObjectSlotMode.FollowPlace;
        Assert.Equal(20, LimitResolver.SlotLimit(s, Kush, Place.DryingRack, "g1"));
    }

    [Fact]
    public void DefinitionLimitIsMaxOverPlaces()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 99;
        s.Places[Place.Deliveries] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 500 };
        Assert.Equal(500, LimitResolver.DefinitionLimit(s, Kush, Array.Empty<string>()));
    }

    [Fact]
    public void DefinitionLimitIncludesObjects()
    {
        var s = ModSettings.CreateDefault();
        s.Objects["g1"] = new ObjectSetting { SlotMode = ObjectSlotMode.Custom, SlotCustom = 700 };
        Assert.Equal(700, LimitResolver.DefinitionLimit(s, Kush, new[] { "g1" }));
    }

    [Fact]
    public void CapacityNeverNegative()
    {
        Assert.Equal(0, LimitResolver.Capacity(10, 15));
        Assert.Equal(6, LimitResolver.Capacity(10, 4));
    }
}

public class InheritedLimitTests
{
    [Fact]
    public void InheritedIgnoresOwnItemOverride()
    {
        var kush = new ItemInfo("ogkush", "Product", 20, false);
        var s = ModSettings.CreateDefault();
        s.Items["ogkush"] = 200;
        Assert.Equal(20, LimitResolver.InheritedItemLimit(s, kush));
        s.Global = 60;
        Assert.Equal(60, LimitResolver.InheritedItemLimit(s, kush));
        s.Categories["Product"] = 80;
        Assert.Equal(80, LimitResolver.InheritedItemLimit(s, kush));
    }

    [Fact]
    public void InheritedForExcludedIsVanilla()
    {
        var mag = new ItemInfo("mag", "Equipment", 1, true);
        var s = ModSettings.CreateDefault();
        s.Global = 60;
        Assert.Equal(1, LimitResolver.InheritedItemLimit(s, mag));
    }
}

public class VehicleTrunkTests
{
    static ItemInfo Kush => new("ogkush", "Product", 20, false);

    [Fact]
    public void TypeOverrideBeatsTrunkSetting()
    {
        var s = ModSettings.CreateDefault();
        s.Places[Place.Vehicles] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 300 };
        var van = ObjectSettings.VehicleKey("veeper");
        Assert.Equal(300, LimitResolver.SlotLimit(s, Kush, Place.Vehicles, van));
        ObjectSettings.Update(s, van, o => { o.SlotMode = ObjectSlotMode.Custom; o.SlotCustom = 50; });
        Assert.Equal(50, LimitResolver.SlotLimit(s, Kush, Place.Vehicles, van));
        Assert.Equal(300, LimitResolver.SlotLimit(s, Kush, Place.Vehicles, ObjectSettings.VehicleKey("shitbox")));
        ObjectSettings.Update(s, van, o => o.SlotMode = ObjectSlotMode.FollowPlace);
        Assert.False(s.Objects.ContainsKey(van));
    }

    [Fact]
    public void OldFilesKeepTrunksOnTheStorageSetting()
    {
        var s = ModSettings.CreateDefault();
        s.Places.Remove(Place.Vehicles);
        s.Places[Place.Storage] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 250 };
        SettingsStore.Normalize(s);
        Assert.Equal(PlaceMode.Custom, s.Places[Place.Vehicles].Mode);
        Assert.Equal(250, s.Places[Place.Vehicles].Custom);
    }
}
