using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SeriesSliderController : MonoBehaviour
{
    // Reference to the main manager script where the logic lives
    public SeriesCircuitManager Manager;

    // Add references to the UI Sliders so we can initialize them
    public List<Slider> ResistorSliders;

    void Start()
    {
        // 1. Attempt to find the Manager component on the same GameObject
        if (Manager == null)
        {
            Manager = GetComponent<SeriesCircuitManager>();
            if (Manager == null)
            {
                Debug.LogError("FATAL ERROR: SeriesCircuitManager component not found. Disabling script.", this);
                enabled = false;
                return;
            }
        }

        // 2. Initial synchronization: Set slider values and add listeners programmatically
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        if (ResistorSliders.Count != Manager.SeriesResistors.Count)
        {
            Debug.LogWarning("Mismatch between Sliders and Resistors. Initialization may fail.", this);
            return;
        }

        for (int i = 0; i < ResistorSliders.Count; i++)
        {
            if (ResistorSliders[i] != null)
            {
                // Set initial value
                ResistorSliders[i].value = Manager.SeriesResistors[i].Resistance;
                
                // IMPORTANT: Add listener from code
                // We create a local variable 'index' to avoid issues with C# closures in loops.
                int index = i; 
                ResistorSliders[i].onValueChanged.AddListener((value) => OnSliderValueChanged(index, value));
            }
        }
    }

       // A single, unified method to handle any slider change
    private void OnSliderValueChanged(int resistorIndex, float newResistanceValue)
    {
        Debug.Log($"Slider {resistorIndex + 1} changed. New value: {newResistanceValue:F2}");
        if (Manager != null)
        {
            Manager.UpdateResistanceAndRecalculate(resistorIndex, newResistanceValue);
        }
    }
}
