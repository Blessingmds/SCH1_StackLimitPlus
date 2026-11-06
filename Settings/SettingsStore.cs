using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCH1_StackLimitPlus.Settings
{
    public class SettingsStore
    {
        public const int MaxStack = 9999;
        public const float MaxCash = 1_000_000f;
        public const int MinWindowWidth = 640, MinWindowHeight = 420, MaxWindowSize = 3840;

        public static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        readonly string _jsonPath;
        readonly string _legacyTxtPath;

        public bool LoadFailed { get; private set; }
        public event Action<string>? Error;

        public SettingsStore(string jsonPath, string legacyTxtPath)
        {
            _jsonPath = jsonPath;
            _legacyTxtPath = legacyTxtPath;
        }

        public ModSettings Load()
        {
            LoadFailed = false;
            ModSettings? s = null;
            if (File.Exists(_jsonPath))
            {
                try { s = JsonSerializer.Deserialize<ModSettings>(File.ReadAllText(_jsonPath), Options); }
                catch (Exception ex)
                {
                    // keep the broken file so the user can fix it; Save() replaces it on the next change
                    LoadFailed = true;
                    Error?.Invoke($"Could not read {_jsonPath}, using defaults: {ex.Message}");
                }
            }
            else if (File.Exists(_legacyTxtPath))
            {
                s = MigrateLegacy();
                Save(s);
            }
            s ??= ModSettings.CreateDefault();
            Normalize(s);
            return s;
        }

        public void Save(ModSettings s)
        {
            try
            {
                var tmp = _jsonPath + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(s, Options));
                File.Move(tmp, _jsonPath, true);
                LoadFailed = false;
            }
            catch (Exception ex)
            {
                Error?.Invoke($"Could not save {_jsonPath}: {ex.Message}");
            }
        }

        ModSettings MigrateLegacy()
        {
            var s = ModSettings.CreateDefault();
            try
            {
                var lines = File.ReadAllLines(_legacyTxtPath);
                if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out var limit)) s.Global = limit;
                var hotkey = lines.FirstOrDefault(l => l.StartsWith("Hotkey=", StringComparison.OrdinalIgnoreCase));
                if (hotkey != null) s.Hotkey = hotkey.Substring("Hotkey=".Length).Trim();
            }
            catch (Exception ex)
            {
                Error?.Invoke($"Could not read old settings {_legacyTxtPath}: {ex.Message}");
            }
            return s;
        }

        public static void Normalize(ModSettings s)
        {
            s.Categories ??= new();
            s.Items ??= new();
            s.Places ??= new();
            s.MixerMaxMix ??= new();
            s.Objects ??= new();
            if (string.IsNullOrWhiteSpace(s.Hotkey)) s.Hotkey = "F6";

            foreach (var kv in ModSettings.DefaultPlaces())
                if (!s.Places.TryGetValue(kv.Key, out var p) || p == null) s.Places[kv.Key] = kv.Value;

            s.Global = Clamp(s.Global);
            ClampAll(s.Categories);
            ClampAll(s.Items);
            ClampAll(s.MixerMaxMix);
            s.DryingRackCapacity = Clamp(s.DryingRackCapacity);
            s.CashPerSlot = ClampCash(s.CashPerSlot);
            s.AtmWeeklyLimit = ClampCash(s.AtmWeeklyLimit);
            if (s.WindowWidth.HasValue) s.WindowWidth = Math.Clamp(s.WindowWidth.Value, MinWindowWidth, MaxWindowSize);
            if (s.WindowHeight.HasValue) s.WindowHeight = Math.Clamp(s.WindowHeight.Value, MinWindowHeight, MaxWindowSize);
            foreach (var p in s.Places.Values) p.Custom = Math.Clamp(p.Custom, 1, MaxStack);
            foreach (var o in s.Objects.Values.Where(o => o != null))
            {
                o.SlotCustom = Math.Clamp(o.SlotCustom, 1, MaxStack);
                o.Capacity = Clamp(o.Capacity);
                o.CashPerSlot = ClampCash(o.CashPerSlot);
            }
        }

        static int? Clamp(int? v) => v.HasValue ? Math.Clamp(v.Value, 1, MaxStack) : null;
        static float? ClampCash(float? v) => v.HasValue ? Math.Clamp(v.Value, 1f, MaxCash) : null;

        static void ClampAll(Dictionary<string, int> d)
        {
            foreach (var k in d.Keys.ToList()) d[k] = Math.Clamp(d[k], 1, MaxStack);
        }
    }
}
