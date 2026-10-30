using System;
using System.Collections.Generic;

namespace SCH1_StackLimitPlus.Settings
{
    // Pure x64 byte matching for CashSlotCap (kept here so the tests can compile it without game references).
    public static class RipFloatScan
    {
        // SSE scalar ops that read a float: movss, addss, mulss, subss, minss, divss, maxss
        static readonly HashSet<byte> Ops = new() { 0x10, 0x58, 0x59, 0x5C, 0x5D, 0x5E, 0x5F };

        // Offsets of the disp32 of every `op xmm, [rip+disp32]` whose target holds `value`.
        // Targets outside [lo, hi) are never read, since a false match could point anywhere.
        public static List<int> Find(byte[] code, long codeAddress, long lo, long hi, Func<long, float> read, float value)
        {
            var hits = new List<int>();
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] != 0xF3) continue;
                int j = i + 1;
                if (j < code.Length && (code[j] & 0xF0) == 0x40) j++; // REX for xmm8-15
                if (j + 7 > code.Length || code[j] != 0x0F || !Ops.Contains(code[j + 1]) || (code[j + 2] & 0xC7) != 0x05) continue;
                long target = codeAddress + j + 7 + BitConverter.ToInt32(code, j + 3);
                if (target >= lo && target + 4 <= hi && read(target) == value) hits.Add(j + 3);
            }
            return hits;
        }
    }
}
