using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SCH1_StackLimitPlus.Settings;

namespace SCH1_StackLimitPlus.Game
{
    // A C# const (cash per slot, ATM weekly limit) is compiled into the game's code as a load of a shared float
    // from read-only data. We repoint exactly those loads, in the named methods, to our own float that settings write.
    public unsafe class CodeConstant
    {
        readonly string _label;
        public float Vanilla { get; }
        float* _value;

        public CodeConstant(string label, float vanilla) { _label = label; Vanilla = vanilla; }

        public void Set(float? value)
        {
            if (_value != null) *_value = value ?? Vanilla;
        }

        // Every method must contain exactly one load of the vanilla value, or nothing is patched.
        public void Install(Type il2cppType, params string[] methods)
        {
            try
            {
                var module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(m => m.ModuleName == "GameAssembly.dll");
                long lo = (long)module.BaseAddress, hi = lo + module.ModuleMemorySize;

                // the next method of the class bounds each scan, so it can't run into unrelated code
                var pointers = il2cppType.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(f => f.Name.StartsWith("NativeMethodInfoPtr_"))
                    .ToDictionary(f => f.Name, f => *(long*)(IntPtr)f.GetValue(null)!);
                var sorted = pointers.Values.Where(p => p != 0).OrderBy(p => p).ToArray();

                var sites = methods.Select(name =>
                {
                    long start = pointers.FirstOrDefault(kv => kv.Key.StartsWith($"NativeMethodInfoPtr_{name}_")).Value;
                    if (start < lo || start >= hi) return (name, site: 0L);
                    long end = Math.Min(sorted.FirstOrDefault(p => p > start, start + 0x4000), hi);
                    var code = new byte[end - start];
                    Marshal.Copy((IntPtr)start, code, 0, code.Length);
                    var hits = RipFloatScan.Find(code, start, lo, hi, a => *(float*)a, Vanilla);
                    return (name, site: hits.Count == 1 ? start + hits[0] : 0L);
                }).ToArray();

                var missing = sites.Where(s => s.site == 0).Select(s => s.name).ToArray();
                if (missing.Length > 0)
                {
                    Core.ErrorOnce($"{_label} is unavailable in this game version (no match in {string.Join(", ", missing)}).");
                    return;
                }

                var value = (float*)AllocNear(lo);
                if (value == null || sites.Any(s => !FitsInt32((long)value - (s.site + 4))))
                {
                    Core.ErrorOnce($"{_label} is unavailable: no memory near the game code.");
                    return;
                }
                *value = Vanilla;
                foreach (var (_, site) in sites) WriteInt32(site, (int)((long)value - (site + 4)));
                _value = value;
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"{_label} is unavailable: {ex.Message}");
            }
        }

        static bool FitsInt32(long v) => v >= int.MinValue && v <= int.MaxValue;

        static void WriteInt32(long address, int v)
        {
            VirtualProtect((IntPtr)address, (UIntPtr)4, PageExecuteReadWrite, out var old);
            *(int*)address = v;
            VirtualProtect((IntPtr)address, (UIntPtr)4, old, out _);
            FlushInstructionCache(GetCurrentProcess(), (IntPtr)address, (UIntPtr)4);
        }

        // rip-relative operands reach +-2 GB, so the float has to live close to GameAssembly
        static IntPtr AllocNear(long target)
        {
            const long step = 0x10000;
            for (long d = step; d < 0x7000_0000; d += step)
                foreach (var a in new[] { target - d, target + d })
                {
                    var p = VirtualAlloc((IntPtr)(a & ~(step - 1)), (UIntPtr)0x1000, MemCommit | MemReserve, PageReadWrite);
                    if (p != IntPtr.Zero) return p;
                }
            return IntPtr.Zero;
        }

        const uint MemCommit = 0x1000, MemReserve = 0x2000, PageReadWrite = 0x04, PageExecuteReadWrite = 0x40;
        [DllImport("kernel32")] static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint type, uint protect);
        [DllImport("kernel32")] static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint protect, out uint old);
        [DllImport("kernel32")] static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
        [DllImport("kernel32")] static extern IntPtr GetCurrentProcess();
    }
}
