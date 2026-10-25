using System.Text.Json;
using SCH1_StackLimitPlus.Settings;
using Xunit;

public class SettingsStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "slp-" + Guid.NewGuid());
    string Json => Path.Combine(_dir, "StackLimitPlus.json");
    string Txt => Path.Combine(_dir, "StackLimitPlus.txt");
    SettingsStore NewStore() => new(Json, Txt);

    public SettingsStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void MissingFilesGiveDefaults()
    {
        var s = NewStore().Load();
        Assert.Null(s.Global);
        Assert.Equal(PlaceMode.Vanilla, s.Places[Place.DryingRack].Mode);
        Assert.Equal("F6", s.Hotkey);
    }

    [Fact]
    public void RoundTrip()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 250; s.Categories["Product"] = 40; s.Items["baggie"] = 999; s.IncludeUnstackable = true;
        s.Places[Place.Deliveries] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 500 };
        s.CashPerSlot = 5000; s.MaxBalance = 20000; s.AtmWeeklyLimit = 50000; s.DryingRackCapacity = 40;
        s.MixerMaxMix["MixingStationMk2"] = 30;
        s.Objects["abc"] = new ObjectSetting { SlotMode = ObjectSlotMode.Custom, SlotCustom = 60, Capacity = 35 };
        s.Hotkey = "F8";
        NewStore().Save(s);
        var loaded = NewStore().Load();
        Assert.Equal(JsonSerializer.Serialize(s), JsonSerializer.Serialize(loaded));
    }

    [Fact]
    public void MigratesLegacyTxt()
    {
        File.WriteAllText(Txt, "250\nHotkey=F7");
        var s = NewStore().Load();
        Assert.Equal(250, s.Global);
        Assert.Equal("F7", s.Hotkey);
        Assert.True(File.Exists(Json));
        Assert.True(File.Exists(Txt));
    }

    [Fact]
    public void LegacyTxtIgnoredWhenJsonExists()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 77;
        NewStore().Save(s);
        File.WriteAllText(Txt, "250\nHotkey=F7");
        Assert.Equal(77, NewStore().Load().Global);
    }

    [Fact]
    public void CorruptJsonGivesDefaultsAndKeepsFile()
    {
        File.WriteAllText(Json, "{ not json");
        var store = NewStore();
        var s = store.Load();
        Assert.Null(s.Global);
        Assert.True(store.LoadFailed);
        Assert.Equal("{ not json", File.ReadAllText(Json));
    }

    [Fact]
    public void ClampsOutOfRange()
    {
        File.WriteAllText(Json, "{\"Global\":0,\"Items\":{\"baggie\":99999},\"CashPerSlot\":-5,\"DryingRackCapacity\":0}");
        var s = NewStore().Load();
        Assert.Equal(1, s.Global);
        Assert.Equal(9999, s.Items["baggie"]);
        Assert.Equal(1, s.CashPerSlot);
        Assert.Equal(1, s.DryingRackCapacity);
    }

    [Fact]
    public void MissingPlacesFilledWithDefaults()
    {
        File.WriteAllText(Json, "{\"Global\":50}");
        var s = NewStore().Load();
        Assert.Equal(PlaceMode.Vanilla, s.Places[Place.Cauldron].Mode);
        Assert.Equal(PlaceMode.ItemLimits, s.Places[Place.Pockets].Mode);
    }

    [Fact]
    public void SaveIsAtomic()
    {
        NewStore().Save(ModSettings.CreateDefault());
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }
}

public class WindowSizeTests
{
    [Fact]
    public void ClampsWindowSize()
    {
        var s = ModSettings.CreateDefault();
        s.WindowWidth = 100; s.WindowHeight = 99999;
        SettingsStore.Normalize(s);
        Assert.Equal(SettingsStore.MinWindowWidth, s.WindowWidth);
        Assert.Equal(SettingsStore.MaxWindowSize, s.WindowHeight);
    }

    [Fact]
    public void WindowSizeIsNotSynced()
    {
        var s = ModSettings.CreateDefault();
        s.WindowWidth = 900; s.WindowHeight = 600;
        var d = SyncCodec.Decode(SyncCodec.Encode(s, 8000, out _));
        Assert.Null(d.WindowWidth);
        Assert.Null(d.WindowHeight);
    }
}
