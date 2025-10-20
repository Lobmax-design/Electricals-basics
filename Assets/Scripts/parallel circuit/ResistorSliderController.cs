using UnityEngine;

public class ResistorSliderController : MonoBehaviour
{
    // Reference to the main calculation manager
    public ParallelCircuitManager manager;

    // We MUST use the Awake method to link the manager automatically if possible
    void Awake()
    {
        // Safety check: try to find the manager if it's not set in the Inspector
        if (manager == null)
        {
            manager = GetComponent<ParallelCircuitManager>();
        }
    }

    // --- These are the functions the UI Sliders will call ---

    public void OnR1Changed(float value)
    {
        // Passes index 0 (R1) and the slider's value to the manager
        if (manager != null)
        {
            manager.UpdateResistanceAndRecalculate(0, value);
        }
    }

    public void OnR2Changed(float value)
    {
        // Passes index 1 (R2) and the slider's value to the manager
        if (manager != null)
        {
            manager.UpdateResistanceAndRecalculate(1, value);
        }
    }

    public void OnR3Changed(float value)
    {
        // Passes index 2 (R3) and the slider's value to the manager
        if (manager != null)
        {
            manager.UpdateResistanceAndRecalculate(2, value);
        }
    }
}

