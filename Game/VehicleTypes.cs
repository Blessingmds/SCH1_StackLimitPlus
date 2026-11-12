using System.Collections.Generic;
using MDSAPI.World;

namespace SCH1_StackLimitPlus.Game
{
    // Every vehicle the game can spawn (code + display name), from the vehicle manager's prefab list.
    public static class VehicleTypes
    {
        public static List<(string code, string name)> All()
        {
            var list = new List<(string, string)>();
            try
            {
                // some cars come in several variants with their own code but the same name: one row per name
                var seen = new HashSet<string>();
                foreach (var v in Vehicles.AllModels())
                {
                    var key = KeyOf(v);
                    if (key.Length > 0 && seen.Add(key)) list.Add((key, v.Name));
                }
            }
            catch (System.Exception ex) { Core.ErrorOnce($"Could not list vehicle types: {ex.Message}"); }
            return list;
        }

        // what a vehicle's trunk settings are stored under (its display name, so variants share one setting)
        public static string KeyOf(Vehicle v) => v.Name.Trim().ToLowerInvariant();
    }
}
