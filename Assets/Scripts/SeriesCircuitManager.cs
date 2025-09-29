using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SeriesCircuitManager : MonoBehaviour
{
    // Reuse the Resistor Data structure
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

    // Call this setup method when the scene starts or a resistor value changes
    public void Start()
    {
        // Initial setup
        CalculateCircuit();
    }

    public void OnResistanceChanged(float value)
    {
        // Note: You must link the UI slider events to this function.
        // If using multiple sliders, pass the index or call a specific function per slider.
        // For simplicity, we assume this is called when ANY R value changes.
        CalculateCircuit();
    }

    public void CalculateCircuit()
    {
        // 1. Calculate Total Resistance (R_total = R1 + R2 + R3 + ...)
        float totalR = 0f;
        foreach (var rData in SeriesResistors)
        {
            totalR += rData.Resistance;
        }

        // 2. Calculate Total Current (I_total = V / R_total)
        float totalI = SourceVoltage / totalR;

        // 3. Calculate Individual Voltage Drops (Vn = I_total * Rn)
        float totalVoltageDropSum = 0f;

        foreach (var rData in SeriesResistors)
        {
            rData.VoltageDrop = totalI * rData.Resistance;
            totalVoltageDropSum += rData.VoltageDrop; // Sum for KVL check

            // 4. Update individual voltmeter displays
            rData.voltageDisplay.text = $"V: {rData.VoltageDrop:F2} V";
        }

        // 5. Update Global Readouts
        totalResistanceDisplay.text = $"R_Total: {totalR:F2} Ω";
        totalCurrentDisplay.text = $"I_Total: {totalI:F2} A";

        // KVL display verifies that the drops equal the source voltage
        kVLDisplay.text = $"KVL Check: V_Source ({SourceVoltage:F2}V) = V_Drops ({totalVoltageDropSum:F2}V)";

        // 6. Update Animation (Constant Current)
        if (CircuitAnimator != null)
        {
            // The entire series circuit animation runs at I_total speed
            CircuitAnimator.SetCurrentFlow(totalI);
        }
    }
}