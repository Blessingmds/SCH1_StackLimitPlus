using System;
using System.Collections.Generic;
using SCH1_StackLimitPlus.Settings;
using Xunit;

public class RipFloatScanTests
{
    const long Code = 0x1000, Lo = 0, Hi = 0x10000;
    static readonly Dictionary<long, float> Mem = new() { [0x2000] = 1000f, [0x3000] = 1f };
    static float Read(long a) => Mem.TryGetValue(a, out var v) ? v : 0f;

    static byte[] Op(params byte[] prefix) => prefix;
    static byte[] Disp(long target, long next) => BitConverter.GetBytes((int)(target - next));

    [Fact]
    public void FindsMovssAndRexMovssReadingTheValue()
    {
        var code = new List<byte>();
        code.AddRange(Op(0xF3, 0x0F, 0x10, 0x05)); code.AddRange(Disp(0x2000, Code + 8));          // movss xmm0,[1000f]
        code.AddRange(Op(0xF3, 0x44, 0x0F, 0x10, 0x0D)); code.AddRange(Disp(0x2000, Code + 17));   // movss xmm9,[1000f]
        code.AddRange(Op(0xF3, 0x0F, 0x10, 0x05)); code.AddRange(Disp(0x3000, Code + 25));         // movss xmm0,[1f]
        var hits = RipFloatScan.Find(code.ToArray(), Code, Lo, Hi, Read, 1000f);
        Assert.Equal(new[] { 4, 13 }, hits);
    }

    [Fact]
    public void IgnoresTargetsOutsideTheModule()
    {
        var code = new List<byte>(Op(0xF3, 0x0F, 0x10, 0x05));
        code.AddRange(Disp(0x2000, Code + 8));
        Assert.Empty(RipFloatScan.Find(code.ToArray(), Code, 0x4000, Hi, a => throw new Exception("read outside module"), 1000f));
    }

    [Fact]
    public void IgnoresNonRipOperands()
    {
        var code = new byte[] { 0xF3, 0x0F, 0x10, 0x43, 0x30, 0, 0, 0 }; // movss xmm0,[rbx+30h]
        Assert.Empty(RipFloatScan.Find(code, Code, Lo, Hi, Read, 1000f));
    }
}
