using System;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;

namespace SCH1_StackLimitPlus.Game
{
    // Frees the cursor and stops camera, movement and equipping while the F6 menu is open.
    public static class InputBlocker
    {
        const string UiName = "StackLimitPlus";
        static bool _blocked;
        static bool _couldMove;

        public static void Block()
        {
            if (_blocked) return;
            try
            {
                var cam = PlayerSingleton<PlayerCamera>.Instance;
                if (cam != null)
                {
                    cam.AddActiveUIElement(UiName);
                    cam.FreeMouse(true);
                    cam.SetCanLook(false);
                }
                var move = PlayerSingleton<PlayerMovement>.Instance;
                if (move != null)
                {
                    _couldMove = move.CanMove;
                    move.CanMove = false;
                }
                PlayerSingleton<PlayerInventory>.Instance?.SetEquippingEnabled(false);
                _blocked = true;
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Could not block game input: {ex.Message}");
            }
        }

        public static void Unblock()
        {
            if (!_blocked) return;
            _blocked = false;
            try
            {
                var cam = PlayerSingleton<PlayerCamera>.Instance;
                if (cam != null)
                {
                    cam.RemoveActiveUIElement(UiName);
                    // another game UI (e.g. storage screen) may still need the cursor
                    if (cam.ActiveUIElementCount == 0)
                    {
                        cam.SetCanLook(true);
                        cam.LockMouse(true);
                    }
                }
                var move = PlayerSingleton<PlayerMovement>.Instance;
                if (move != null) move.CanMove = _couldMove;
                PlayerSingleton<PlayerInventory>.Instance?.SetEquippingEnabled(true);
            }
            catch (Exception ex)
            {
                Core.ErrorOnce($"Could not restore game input: {ex.Message}");
            }
        }

        // scene change destroys the player; forget state without touching it
        public static void Reset() => _blocked = false;
    }
}
