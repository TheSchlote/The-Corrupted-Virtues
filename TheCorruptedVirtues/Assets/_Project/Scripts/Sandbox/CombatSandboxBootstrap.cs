using UnityEngine;

namespace TheCorruptedVirtues.Combat
{
    // Creates a minimal UI harness for the combat sandbox at runtime.
    public sealed class CombatSandboxBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            SwingMeterUiFactory.SwingMeterUi ui = SwingMeterUiFactory.Build();
            CombatSandboxController controller = gameObject.AddComponent<CombatSandboxController>();
            controller.SetReferences(ui.Slider, ui.Text);
        }
    }
}
