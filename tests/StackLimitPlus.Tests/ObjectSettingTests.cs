using SCH1_StackLimitPlus.Settings;
using Xunit;

public class ObjectSettingTests
{
    [Fact]
    public void UpdateCreatesEntry()
    {
        var s = ModSettings.CreateDefault();
        ObjectSettings.Update(s, "g1", o => o.Capacity = 30);
        Assert.Equal(30, s.Objects["g1"].Capacity);
        Assert.Equal(ObjectSlotMode.FollowPlace, s.Objects["g1"].SlotMode);
    }

    [Fact]
    public void UpdateBackToDefaultsRemovesEntry()
    {
        var s = ModSettings.CreateDefault();
        ObjectSettings.Update(s, "g1", o => { o.SlotMode = ObjectSlotMode.Custom; o.SlotCustom = 40; });
        ObjectSettings.Update(s, "g1", o => o.SlotMode = ObjectSlotMode.FollowPlace);
        Assert.False(s.Objects.ContainsKey("g1"));
    }

    [Fact]
    public void CashPerSlotKeepsEntryUntilCleared()
    {
        var s = ModSettings.CreateDefault();
        ObjectSettings.Update(s, "bed", o => o.CashPerSlot = 5000);
        Assert.Equal(5000, s.Objects["bed"].CashPerSlot);
        ObjectSettings.Update(s, "bed", o => o.CashPerSlot = null);
        Assert.False(s.Objects.ContainsKey("bed"));
    }

    [Fact]
    public void GetReturnsDefaultsWithoutAdding()
    {
        var s = ModSettings.CreateDefault();
        var o = ObjectSettings.Get(s, "nope");
        Assert.Equal(ObjectSlotMode.FollowPlace, o.SlotMode);
        Assert.Null(o.Capacity);
        Assert.Empty(s.Objects);
    }
}
