using UnityEngine;
using System.Collections.Generic;
using TMPro;

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
        public CurrentAnimator animatorReference; // Dedicated animator for this branch
        [HideInInspector] public float BranchCurrent; // Calculated I = V / R
    }

    [Header("Circuit Constants")]
    public float SourceVoltage = 12f;

    [Header("Parallel Branches & Animation")]
    public List<ResistorData> ParallelResistors; // Assign R1, R2, R3 data here
    public CurrentAnimator TotalCurrentAnimator; // Animator for the main wire

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

    public void OnResistanceChanged(float value)
    {
        // Re-calculate when user interacts with a slider
        CalculateCircuit();
    }

    public void CalculateCircuit()
    {
        // 1. Calculate Total Resistance (Reciprocal Sum)
        float reciprocalRSum = 0f;
        foreach (var rData in ParallelResistors)
        {
            // Ensure R is not zero to prevent error
            reciprocalRSum += 1f / Mathf.Max(rData.Resistance, 0.01f);
        }
        float totalR = 1f / reciprocalRSum;

        // 2. Voltage is Constant (V_branch = V_source)
        float branchVoltage = SourceVoltage;
        float totalI = 0f;

        // 3. Calculate Individual Branch Currents (In = V / Rn)
        foreach (var rData in ParallelResistors)
        {
            rData.BranchCurrent = branchVoltage / rData.Resistance;
            totalI += rData.BranchCurrent; // Sum currents for KCL

            // Update individual displays (Constant V, Branch I)
            rData.voltageDisplay.text = $"V: {branchVoltage:F2} V";
            rData.currentDisplay.text = $"I: {rData.BranchCurrent:F2} A";

            // Update the dedicated branch animator speed
            if (rData.animatorReference != null)
            {
                rData.animatorReference.SetCurrentFlow(rData.BranchCurrent);
            }
        }

        // 4. Update Global Readouts
        totalResistanceDisplay.text = $"R_Total: {totalR:F2} Ω";
        totalCurrentDisplay.text = $"I_Total: {totalI:F2} A";

        // KCL display verifies current summation
        KCLDisplay.text = $"KCL Check: I_Total ({totalI:F2}A) = Sum of I_Branches";

        // 5. Update Total Current Animation
        if (TotalCurrentAnimator != null)
        {
            // Main wire animation speed reflects the total current
            TotalCurrentAnimator.SetCurrentFlow(totalI);
        }
    }
}
