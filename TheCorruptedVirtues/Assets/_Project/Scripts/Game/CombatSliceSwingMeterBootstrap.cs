using UnityEngine;

namespace TheCorruptedVirtues.CombatSlice.Unity
{
    // Builds the shared swing meter UI and wires it into the combat slice.
    public sealed class CombatSliceSwingMeterBootstrap : MonoBehaviour
    {
        [SerializeField] private CombatSliceController combatSliceController;
        [SerializeField] private Transform uiParent;

        private void Awake()
        {
            if (combatSliceController == null)
            {
                combatSliceController = FindObjectOfType<CombatSliceController>();
            }

            if (combatSliceController == null)
            {
                Debug.LogError("CombatSliceSwingMeterBootstrap could not find CombatSliceController.");
                return;
            }

            SwingMeterUiFactory.SwingMeterUi ui = SwingMeterUiFactory.Build(uiParent);
            SwingMeterController meter = ui.Root.AddComponent<SwingMeterController>();
            meter.SetReferences(ui.Root, ui.Slider, ui.Text);
            combatSliceController.SetSwingMeter(meter);
        }
    }
}
