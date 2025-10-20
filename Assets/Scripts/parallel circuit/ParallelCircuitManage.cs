using UnityEngine;
using System.Collections.Generic;
using TMPro; // Ensure you have this using directive for TextMeshProUGUI

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
        if (resistorIndex >= 0 && resistorIndex < ParallelResistors.Count)
        {
            // 1. Update the internal data structure with the new value
            // We use Mathf.Max to ensure resistance can never be negative.
            ParallelResistors[resistorIndex].Resistance = Mathf.Max(newResistanceValue, 0.01f);

            // 2. Immediately recalculate the entire circuit based on the change
            CalculateCircuit();
        }
        else
        {
            Debug.LogError($"Invalid resistor index ({resistorIndex}) passed from slider.");
        }
    }


    public void CalculateCircuit()
    {
        // Define a minimum resistance threshold to prevent division by zero (short circuit)
        const float MIN_RESISTANCE = 0.01f;
        float totalVoltageDropSum = 0f; // For KVL check, though V is constant in parallel.

        // 1. Calculate Total Resistance (Reciprocal Sum)
        float reciprocalRSum = 0f;
        foreach (var rData in ParallelResistors)
        {
            // Use Mathf.Max to ensure resistance is never zero when dividing
            float safeResistance = Mathf.Max(rData.Resistance, MIN_RESISTANCE);
            reciprocalRSum += 1f / safeResistance;
        }
        float totalR = 1f / reciprocalRSum;

        // 2. Voltage is Constant (V_branch = V_source)
        float branchVoltage = SourceVoltage;
        float totalI = 0f;

        // 3. Calculate Individual Branch Currents (In = V / Rn)
        foreach (var rData in ParallelResistors)
        {
            // Use the actual resistance value for display, but safe resistance for calculation
            float safeResistance = Mathf.Max(rData.Resistance, MIN_RESISTANCE);

            rData.BranchCurrent = branchVoltage / safeResistance;
            totalI += rData.BranchCurrent; // Sum currents for KCL

            // Update individual displays (Constant V, Branch I)
            // NOTE: We only display V_source, but the calculation needs safeR.
            rData.voltageDisplay.text = $"V: {branchVoltage:F2} V";
            rData.currentDisplay.text = $"I: {rData.BranchCurrent:F2} A";

            // Update the dedicated branch animator speed
            if (rData.animatorReference != null)
            {
                rData.animatorReference.SetCurrentFlow(rData.BranchCurrent);
            }
        }

        // 4. Update Global Readouts and Animation
        totalResistanceDisplay.text = $"R_Total: {totalR:F2} Ω";
        totalCurrentDisplay.text = $"I_Total: {totalI:F2} A";

        // KCL display verifies current summation
        KCLDisplay.text = $"KCL Check: I_Total ({totalI:F2}A) = Sum of I_Branches ({totalI:F2}A)";

        if (TotalCurrentAnimator != null)
        {
            // Main wire animation speed reflects the total current
            TotalCurrentAnimator.SetCurrentFlow(totalI);
        }
    }
}
