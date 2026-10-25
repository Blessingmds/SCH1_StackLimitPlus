using SCH1_StackLimitPlus.Settings;
using Xunit;

public class PlacePresetTests
{
    [Fact]
    public void PocketsOnlySetsEverythingElseVanilla()
    {
        var s = ModSettings.CreateDefault();
        s.Places[Place.Deliveries] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 500 };
        PlacePresets.Apply(s, PlacePreset.PocketsOnly);
        Assert.Equal(PlaceMode.ItemLimits, s.Places[Place.Pockets].Mode);
        Assert.Equal(PlaceMode.Vanilla, s.Places[Place.Storage].Mode);
        Assert.Equal(PlaceMode.Vanilla, s.Places[Place.Deliveries].Mode);
        Assert.Equal(500, s.Places[Place.Deliveries].Custom); // keeps the typed number for later
    }

    [Fact]
    public void ExceptStationsMatchesDefaults()
    {
        var s = ModSettings.CreateDefault();
        PlacePresets.Apply(s, PlacePreset.Everywhere);
        PlacePresets.Apply(s, PlacePreset.ExceptStations);
        foreach (var kv in ModSettings.DefaultPlaces()) Assert.Equal(kv.Value.Mode, s.Places[kv.Key].Mode);
    }

    [Fact]
    public void EverywhereUsesItemLimits()
    {
        var s = ModSettings.CreateDefault();
        PlacePresets.Apply(s, PlacePreset.Everywhere);
        Assert.All(s.Places.Values, p => Assert.Equal(PlaceMode.ItemLimits, p.Mode));
    }

    [Fact]
    public void DetectsCurrentPreset()
    {
        var s = ModSettings.CreateDefault();
        Assert.Equal(PlacePreset.ExceptStations, PlacePresets.Current(s));
        s.Places[Place.Storage].Mode = PlaceMode.Custom;
        Assert.Null(PlacePresets.Current(s));
    }
}
