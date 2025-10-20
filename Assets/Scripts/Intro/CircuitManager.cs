using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CircuitManager : MonoBehaviour
{
    // --- Public Properties (Set in Inspector) ---
    [Header("Circuit Constants")]
    public float SourceVoltage = 10f; // V is constant for this step

    [Header("Resistor R1")]
    public float ResistanceR1 = 10f; // Initial R1 value
    public Slider resistanceSlider;   // Reference to the UI Slider
    public float minResistance = 1f;
    public float maxResistance = 50f;

    [Header("Readouts and Animation")]
    public TextMeshProUGUI currentDisplay; // Ammeter readout
    public CurrentAnimator currentAnimator; // Reference to the script controlling the electrons
    public TextMeshProUGUI analogyText;      // Analogy explanation

    private float current; // Calculated current (I)

    void Start()
    {
        // 1. Initialize the Slider
        if (resistanceSlider != null)
        {
            resistanceSlider.minValue = minResistance;
            resistanceSlider.maxValue = maxResistance;
            resistanceSlider.value = ResistanceR1;
            resistanceSlider.onValueChanged.AddListener(OnResistanceChanged);
        }

        // 2. Perform initial calculation and update visuals
        CalculateSingleResistorCircuit();
    }

    // Called whenever the user moves the slider
    void OnResistanceChanged(float newR)
    {
        ResistanceR1 = newR;
        CalculateSingleResistorCircuit();
    }

    void CalculateSingleResistorCircuit()
    {
        // --- 3. CORE CALCULATION: OHM'S LAW (I = V / R) ---
        // Prevents division by zero if R is very close to zero
        current = SourceVoltage / Mathf.Max(ResistanceR1, 0.01f);

        // --- 4. UPDATE READOUTS AND ANIMATION ---
        UpdateCurrentVisualization();
    }

    void UpdateCurrentVisualization()
    {
        // Update Ammeter Display
        currentDisplay.text = $"Current (I): {current:F2} A";

        // Update Current Animation Speed
        if (currentAnimator != null)
        {
            // Pass the calculated current value to the animator
            currentAnimator.SetCurrentFlow(current);
        }

        // Update Analogy Text (Qualitative Visualization)
        if (ResistanceR1 > maxResistance / 2) // Check if R1 is high
        {
            analogyText.text = "High Resistance: Like a thin pipe, current flow is slow.";
        }
        else
        {
            analogyText.text = "Low Resistance: Like a wide pipe, current flow is fast.";
        }
    }
}