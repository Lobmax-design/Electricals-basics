using UnityEngine;
using System.Collections.Generic;
using TMPro; // Ensure you have this using directive for TextMeshProUGUI
using System;

public class ParallelCircuitManager : MonoBehaviour
{
    // Reusing the data structure from the Series scene
    [System.Serializable]
    public class ResistorData
    {
        public string Name;
        public float Resistance = 10f;
        public TextMeshProUGUI voltageDisplay;
        public TextMeshProUGUI currentDisplay; // Ammeter for this branch
        public ParallelCircuitAnimator animatorReference; // Dedicated animator for this branch
        [HideInInspector] public float BranchCurrent; // Calculated I = V / R
    }

    [Header("Circuit Constants")]
    public float SourceVoltage = 12f;

    [Header("Parallel Branches & Animation")]
    public List<ResistorData> ParallelResistors; // Assign R1, R2, R3 data here
    public ParallelCircuitAnimator TotalCurrentAnimator; // Animator for the main wire

    [Header("Optional: Junction Splitter")]
    // Assign the JunctionSplitter here so it can be updated whenever currents change

    [Header("UI Readouts")]
    public TextMeshProUGUI totalResistanceDisplay;
    public TextMeshProUGUI totalCurrentDisplay;
    public TextMeshProUGUI KCLDisplay; // For Itotal = I1 + I2 + I3

    // Call this when scene starts or a resistor value changes
    public void Start()
    {
        // Link UI sliders here if needed, then run calculation
        CalculateCircuit();
    }

    // --- NEW METHOD TO HANDLE SLIDER INPUT (Accepts Index and Value) ---
    // This function will be linked to the "On Value Changed" event of the sliders.
    public void UpdateResistanceAndRecalculate(int resistorIndex, float newResistanceValue)
    {
        Debug.Log($"[Manager] UpdateResistanceAndRecalculate idx={resistorIndex} val={newResistanceValue:F3}");
        if (resistorIndex >= 0 && resistorIndex < ParallelResistors.Count)
        {
            ParallelResistors[resistorIndex].Resistance = Mathf.Max(newResistanceValue, 0.01f);
            // show current resistances
            for (int i = 0; i < ParallelResistors.Count; i++)
                Debug.Log($"[Manager] R{i+1} = {ParallelResistors[i].Resistance:F3}");
            CalculateCircuit();
        }
        else
        {
            Debug.LogError($"Invalid resistor index ({resistorIndex}) passed from slider.");
        }
    }


    public event Action CurrentsUpdated;

    public void CalculateCircuit()
    {
        const float MIN_RESISTANCE = 0.01f;
        float reciprocalRSum = 0f;
        foreach (var rData in ParallelResistors)
        {
            float safeResistance = Mathf.Max(rData.Resistance, MIN_RESISTANCE);
            reciprocalRSum += 1f / safeResistance;
        }
        float totalR = (reciprocalRSum > 0f) ? (1f / reciprocalRSum) : float.PositiveInfinity;
        float branchVoltage = SourceVoltage;
        float totalI = 0f;

        for (int i = 0; i < ParallelResistors.Count; i++)
        {
            var rData = ParallelResistors[i];
            float safeResistance = Mathf.Max(rData.Resistance, MIN_RESISTANCE);
            rData.BranchCurrent = branchVoltage / safeResistance;
            totalI += rData.BranchCurrent;

            // Update individual displays (Constant V, Branch I)
            rData.voltageDisplay.text = $"V: {branchVoltage:F2} V";
            rData.currentDisplay.text = $"I: {rData.BranchCurrent:F2} A";

            // Update the dedicated branch animator speed
            if (rData.animatorReference != null)
            {
                rData.animatorReference.SetCurrentFlow(rData.BranchCurrent);
            }

            Debug.Log($"[Manager] Branch {i+1}: R={rData.Resistance:F3} Ohm -> I={rData.BranchCurrent:F4} A");
        }

        totalResistanceDisplay.text = $"R_Total: {totalR:F2} Ω";
        totalCurrentDisplay.text = $"I_Total: {totalI:F2} A";
        KCLDisplay.text = $"KCL Check: I_Total ({totalI:F2}A) = Sum of I_Branches ({totalI:F2}A)";

        Debug.Log($"[Manager] TotalR={totalR:F4} Ω TotalI={totalI:F4} A");

        if (TotalCurrentAnimator != null)
        {
            TotalCurrentAnimator.SetCurrentFlow(totalI);
        }

        // Notify all splitters and listeners that currents have been updated
        if (CurrentsUpdated != null)
            CurrentsUpdated();
    }
}
