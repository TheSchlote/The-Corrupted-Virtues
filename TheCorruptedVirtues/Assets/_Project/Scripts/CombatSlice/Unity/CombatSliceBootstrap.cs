using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    public static class CombatSliceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureControllerExists()
        {
            CombatSliceController existing = Object.FindFirstObjectByType<CombatSliceController>();
            if (existing != null)
            {
                return;
            }

            var controller = new GameObject("CombatSliceController");
            controller.AddComponent<CombatSliceController>();
        }
    }
}
