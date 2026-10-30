using SCH1_StackLimitPlus.Settings;
using Xunit;

public class AtmLimitTests
{
    [Fact]
    public void UnsetKeepsGameLimit() => Assert.True(AtmLimit.GameLimitEnabled(null, 10000, 50000));

    [Fact]
    public void BelowVanillaKeepsGameLimit() => Assert.True(AtmLimit.GameLimitEnabled(5000, 10000, 0));

    [Fact]
    public void RaisedLimitLiftsGameLimitUntilReached()
    {
        Assert.False(AtmLimit.GameLimitEnabled(20000, 10000, 15000));
        Assert.True(AtmLimit.GameLimitEnabled(20000, 10000, 20000));
    }
}
