using System;

namespace SCH1_StackLimitPlus.Settings
{
    public static class ObjectSettings
    {
        public static ObjectSetting Get(ModSettings s, string guid) =>
            s.Objects.TryGetValue(guid, out var o) ? o : new ObjectSetting();

        // Changes one object's setting; an entry back at defaults is removed so the file doesn't fill with no-ops.
        public static void Update(ModSettings s, string guid, Action<ObjectSetting> change)
        {
            var o = Get(s, guid);
            change(o);
            if (o.SlotMode == ObjectSlotMode.FollowPlace && o.Capacity == null) s.Objects.Remove(guid);
            else s.Objects[guid] = o;
        }
    }
}
