namespace SCH1_StackLimitPlus.Settings
{
    // The game's weekly ATM limit is a compiled-in constant; it can only be switched off.
    // A raised limit = switch the game's limit off until this week's deposits reach the custom amount.
    public static class AtmLimit
    {
        public static bool GameLimitEnabled(float? custom, float vanilla, float weeklySum) =>
            custom == null || custom <= vanilla || weeklySum >= custom;
    }
}
