using System.Collections.Generic;
using Il2CppScheduleOne.Vehicles;

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
                var manager = UnityEngine.Object.FindObjectOfType<VehicleManager>();
                if (manager?.VehiclePrefabs == null) return list;
                // some cars come in several variants with their own code but the same name: one row per name
                var seen = new HashSet<string>();
                foreach (var v in manager.VehiclePrefabs)
                {
                    if (v == null) continue;
                    var key = KeyOf(v);
                    if (key.Length > 0 && seen.Add(key)) list.Add((key, NameOf(v)));
                }
            }
            catch (System.Exception ex) { Core.ErrorOnce($"Could not list vehicle types: {ex.Message}"); }
            return list;
        }

        static string NameOf(LandVehicle v) => string.IsNullOrEmpty(v.VehicleName) ? v.VehicleCode ?? "" : v.VehicleName;

        // what a vehicle's trunk settings are stored under (its display name, so variants share one setting)
        public static string KeyOf(LandVehicle v) => NameOf(v).Trim().ToLowerInvariant();
    }
}
