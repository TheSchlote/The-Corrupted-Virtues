namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Single access point for game input. Defaults to the Unity Input System
    // backend; assign Current to swap it (tests, replay, future remapping)
    // without controllers knowing or caring. PR D's code-driven bootstrap is
    // the natural place to formalize injection if needed.
    public static class GameInput
    {
        private static IGameInput current = new UnitySystemInput();

        public static IGameInput Current
        {
            get { return current; }
            set { current = value ?? new UnitySystemInput(); }
        }
    }
}
