using System.Text;
using SCH1_StackLimitPlus.Settings;
using Xunit;

public class SyncCodecTests
{
    [Fact]
    public void RoundTripWithoutHotkey()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 120; s.Categories["Packaging"] = 300; s.Items["baggie"] = 999;
        s.Places[Place.Storage] = new PlaceSetting { Mode = PlaceMode.Custom, Custom = 250 };
        s.CashPerSlot = 4000; s.AtmWeeklyLimit = 40000; s.DryingRackCapacity = 40; s.MixerMaxMix["MixingStation"] = 25;
        s.Objects["g1"] = new ObjectSetting { SlotMode = ObjectSlotMode.Vanilla, Capacity = 10 };
        s.Hotkey = "F9";

        var d = SyncCodec.Decode(SyncCodec.Encode(s, 8000, out var truncated));

        Assert.False(truncated);
        Assert.Equal(120, d.Global);
        Assert.Equal(300, d.Categories["Packaging"]);
        Assert.Equal(999, d.Items["baggie"]);
        Assert.Equal(PlaceMode.Custom, d.Places[Place.Storage].Mode);
        Assert.Equal(250, d.Places[Place.Storage].Custom);
        Assert.Equal(40000, d.AtmWeeklyLimit);
        Assert.Equal(4000, d.CashPerSlot);
        Assert.Equal(40, d.DryingRackCapacity);
        Assert.Equal(25, d.MixerMaxMix["MixingStation"]);
        Assert.Equal(ObjectSlotMode.Vanilla, d.Objects["g1"].SlotMode);
        Assert.Equal(10, d.Objects["g1"].Capacity);
        Assert.Equal("F6", d.Hotkey);
    }

    [Fact]
    public void DropsObjectsThenItemsWhenTooLarge()
    {
        var s = ModSettings.CreateDefault();
        s.Global = 64;
        s.Categories["Product"] = 32;
        var rng = new Random(1);
        for (int i = 0; i < 2000; i++) s.Items[Guid.NewGuid().ToString("N")] = rng.Next(1, 9999);
        for (int i = 0; i < 500; i++) s.Objects[Guid.NewGuid().ToString()] = new ObjectSetting { SlotMode = ObjectSlotMode.Custom, SlotCustom = rng.Next(1, 9999) };

        var data = SyncCodec.Encode(s, 8000, out var truncated);
        var d = SyncCodec.Decode(data);

        Assert.True(truncated);
        Assert.True(Encoding.UTF8.GetByteCount(data) <= 8000);
        Assert.Empty(d.Objects);
        Assert.Equal(64, d.Global);
        Assert.Equal(32, d.Categories["Product"]);
        Assert.Equal(PlaceMode.Vanilla, d.Places[Place.BrickPress].Mode);
    }

    [Fact]
    public void DecodeGarbageReturnsDefaults()
    {
        var d = SyncCodec.Decode("not base64 at all!!");
        Assert.Null(d.Global);
        Assert.Equal(PlaceMode.Vanilla, d.Places[Place.DryingRack].Mode);
    }
}
