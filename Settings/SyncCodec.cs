using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SCH1_StackLimitPlus.Settings
{
    // Host -> client settings for Steam lobby data: JSON, deflated, base64.
    public static class SyncCodec
    {
        static readonly JsonSerializerSettings Compact = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() },
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        public static string Encode(ModSettings s, int maxBytes, out bool truncated)
        {
            truncated = false;
            var copy = JsonConvert.DeserializeObject<ModSettings>(JsonConvert.SerializeObject(s, Compact), Compact)!;
            copy.Hotkey = null!; // the hotkey and window size are personal, never synced
            copy.WindowWidth = copy.WindowHeight = null;

            var data = Pack(copy);
            if (data.Length <= maxBytes) return data;

            truncated = true;
            copy.Objects.Clear();
            data = Pack(copy);
            if (data.Length <= maxBytes) return data;

            copy.Items.Clear();
            return Pack(copy);
        }

        public static ModSettings Decode(string data)
        {
            ModSettings? s = null;
            try
            {
                using var input = new MemoryStream(Convert.FromBase64String(data));
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                using var reader = new StreamReader(deflate, Encoding.UTF8);
                s = JsonConvert.DeserializeObject<ModSettings>(reader.ReadToEnd(), Compact);
            }
            catch (Exception) { }
            s ??= ModSettings.CreateDefault();
            s.Hotkey = "F6";
            SettingsStore.Normalize(s);
            return s;
        }

        static string Pack(ModSettings s)
        {
            var json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(s, Compact));
            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
                deflate.Write(json, 0, json.Length);
            return Convert.ToBase64String(output.ToArray());
        }
    }
}
