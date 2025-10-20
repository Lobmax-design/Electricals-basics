using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SeriesCircuitManager : MonoBehaviour
{
    // Define minimum resistance outside the loop to be used for safety
    private const float MIN_RESISTANCE = 0.01f;

    [System.Serializable]
    public class ResistorData
    {
        public string Name;
        public float Resistance = 10f;
        public TextMeshProUGUI voltageDisplay; // V1, V2, V3 meter displays
        [HideInInspector] public float VoltageDrop;
    }

    [Header("Circuit Constants")]
    public float SourceVoltage = 12f;

    [Header("Components & Animation")]
    public List<ResistorData> SeriesResistors; // Assign R1, R2, R3 here
    public CurrentAnimator CircuitAnimator; // The electron animator for the whole circuit

    [Header("UI Readouts")]
    public TextMeshProUGUI totalResistanceDisplay;
    public TextMeshProUGUI totalCurrentDisplay;
    public TextMeshProUGUI kVLDisplay; // For Vtotal = V1 + V2 + V3

    public void Start()
    {
        // Initial setup
        CalculateCircuit();
    }

    // --- CRITICAL FUNCTION: Receives dynamic data from the Slider Controller ---
    public void UpdateResistanceAndRecalculate(int resistorIndex, float newResistanceValue)
    {
        // 1. Safety check and bounds check
        if (resistorIndex >= 0 && resistorIndex < SeriesResistors.Count)
        {
            // 2. UPDATE THE RESISTANCE VALUE IN THE DATA LIST
            // We ensure resistance cannot be negative or near zero.
            SeriesResistors[resistorIndex].Resistance = Mathf.Max(newResistanceValue, MIN_RESISTANCE);

            // 3. Immediately trigger the calculation with the new value
            CalculateCircuit();
        }
        else
        {
            Debug.LogError($"Invalid resistor index ({resistorIndex}) passed from slider.");
        }
    }


    // --- DELETED: Removed the useless OnResistanceChanged(float value) placeholder ---


    public void CalculateCircuit()
    {
        // 1. Calculate Total Resistance (R_total = R1 + R2 + R3 + ...)
        float totalR = 0f;
        foreach (var rData in SeriesResistors)
        {
            // Use safe resistance value for calculation
            totalR += Mathf.Max(rData.Resistance, MIN_RESISTANCE);
        }

        // 2. Calculate Total Current (I_total = V / R_total)
        float totalI = SourceVoltage / totalR;

        // --- Error Guardrail Check ---
        if (float.IsInfinity(totalI) || float.IsNaN(totalI))
        {
            totalI = 0f;
        }

        // 3. Calculate Individual Voltage Drops (Vn = I_total * Rn)
        float totalVoltageDropSum = 0f;

        foreach (var rData in SeriesResistors)
        {
            float safeResistance = Mathf.Max(rData.Resistance, MIN_RESISTANCE);

            // Calculate voltage drop 
            rData.VoltageDrop = totalI * safeResistance;
            totalVoltageDropSum += rData.VoltageDrop;

            // 4. Update individual voltmeter displays
            rData.voltageDisplay.text = $"V: {rData.VoltageDrop:F2} V";
        }

        // 5. Update Global Readouts
        totalResistanceDisplay.text = $"R_Total: {totalR:F2} Ω";
        totalCurrentDisplay.text = $"I_Total: {totalI:F2} A";

        // KVL display verifies that the drops equal the source voltage
        kVLDisplay.text = $"KVL Check: V_Source ({SourceVoltage:F2}V) ≈ V_Drops ({totalVoltageDropSum:F2}V)";

        // 6. Update Animation (Constant Current)
        if (CircuitAnimator != null)
        {
            // The entire series circuit animation runs at I_total speed
            CircuitAnimator.SetCurrentFlow(totalI);
        }
    }
}
