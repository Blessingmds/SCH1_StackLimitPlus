using MDSAPI.Players;

namespace SCH1_StackLimitPlus.Game
{
    // Frees the cursor and stops camera, movement and equipping while the F6 menu is open.
    // MDSAPI does the work, and puts everything back only once no other screen still needs the cursor.
    public static class InputBlocker
    {
        const string UiName = "StackLimitPlus";

        public static void Block() => PlayerControls.Block(UiName);

        public static void Unblock() => PlayerControls.Unblock(UiName);
    }
}
