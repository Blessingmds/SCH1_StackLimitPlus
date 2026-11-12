using System;
using MDSAPI.Multiplayer;
using MelonLoader;
using SCH1_StackLimitPlus.Settings;
using UnityEngine;

namespace SCH1_StackLimitPlus.Game
{
    // Multiplayer: the host publishes its settings as Steam lobby data; clients apply them in memory, read-only.
    public static class LobbySync
    {
        const string Key = "StackLimitPlus";
        const int MaxBytes = 8000;
        const float PollSeconds = 3f;

        static float _timer;
        static bool _dirty = true;
        static string? _lastReceived;

        public static void MarkDirty() => _dirty = true;

        public static void Tick()
        {
            if ((_timer -= Time.unscaledDeltaTime) > 0) return;
            _timer = PollSeconds;
            try
            {
                if (!Lobby.InLobby)
                {
                    if (Core.IsClient) Core.RestoreLocalSettings();
                    _lastReceived = null;
                    _dirty = true; // publish again when hosting a new lobby
                    return;
                }

                if (Lobby.IsHost)
                {
                    if (!_dirty) return;
                    _dirty = false;
                    var data = SyncCodec.Encode(Core.Settings, MaxBytes, out var truncated);
                    if (truncated) Core.ErrorOnce("Too many per-item/per-object settings to share with other players; those stay host-only.");
                    Lobby.SetData(Key, data);
                    return;
                }

                var received = Lobby.GetData(Key);
                if (string.IsNullOrEmpty(received))
                {
                    // host doesn't run the mod (or hasn't published yet): keep our own settings
                    if (Core.IsClient) Core.RestoreLocalSettings();
                    _lastReceived = null;
                    return;
                }
                if (received == _lastReceived) return;
                _lastReceived = received;
                Core.ApplyHostSettings(SyncCodec.Decode(received!));
                MelonLogger.Msg("Using the host's StackLimit Plus settings.");
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Multiplayer settings sync failed: {ex.Message}");
            }
        }
    }
}
