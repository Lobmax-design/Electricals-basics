using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Jobs;

public class ResistorSliderController : MonoBehaviour
{
    public ParallelCircuitManager manager;

    public Slider R1Slider;
    public Slider R2Slider;
    public Slider R3Slider;

    public bool invertR1 = false;
    public bool invertR2 = false;
    public bool invertR3 = false;

    private bool isSyncing = false; // 🔒 Guard flag to prevent recursion

    void Awake()
    {
        if (manager == null)
        {
            manager = GetComponent<ParallelCircuitManager>();
            if (manager == null)
                manager = FindObjectOfType<ParallelCircuitManager>();
        }
    }

    void OnEnable()
    {
        if (manager != null)
            manager.CurrentsUpdated += SyncSlidersWithManager;
    }

    void Start()
    {
        // Ensure no duplicate listeners
        if (R1Slider != null)
        {
            R1Slider.onValueChanged.RemoveAllListeners();
            R1Slider.onValueChanged.AddListener(OnR1Changed);
        }
        if (R2Slider != null)
        {
            R2Slider.onValueChanged.RemoveAllListeners();
            R2Slider.onValueChanged.AddListener(OnR2Changed);
        }
        if (R3Slider != null)
        {
            R3Slider.onValueChanged.RemoveAllListeners();
            R3Slider.onValueChanged.AddListener(OnR3Changed);
        }

        // Initialize to match manager values
        SyncSlidersWithManager();
    }

    void OnDisable()
    {
        if (manager != null)
            manager.CurrentsUpdated -= SyncSlidersWithManager;
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.CurrentsUpdated -= SyncSlidersWithManager;
    }

    public void SyncSlidersWithManager()
    {
        if (manager == null || manager.ParallelResistors == null) return;

        isSyncing = true; // 🧩 prevent slider event feedback

        if (R1Slider != null && manager.ParallelResistors.Count > 0)
            R1Slider.value = MapResistanceToSlider(R1Slider, manager.ParallelResistors[0].Resistance, invertR1);

        if (R2Slider != null && manager.ParallelResistors.Count > 1)
            R2Slider.value = MapResistanceToSlider(R2Slider, manager.ParallelResistors[1].Resistance, invertR2);

        if (R3Slider != null && manager.ParallelResistors.Count > 2)
            R3Slider.value = MapResistanceToSlider(R3Slider, manager.ParallelResistors[2].Resistance, invertR3);

        isSyncing = false; // ✅ re-enable normal callbacks
    }

    private float MapSliderToResistance(Slider slider, float rawValue, bool invert)
    {
        if (slider == null) return rawValue;
        float min = slider.minValue;
        float max = slider.maxValue;
        float mapped = rawValue;
        if (invert)
            mapped = min + max - rawValue;
        return Mathf.Max(mapped, 0.01f);
    }

    private float MapResistanceToSlider(Slider slider, float resistance, bool invert)
    {
        if (slider == null) return resistance;
        float min = slider.minValue;
        float max = slider.maxValue;
        float value = Mathf.Clamp(resistance, min, max);
        if (invert)
            value = min + max - value;
        return value;
    }

    public void OnR1Changed(float value)
    {
        if (isSyncing) return; // 🚫 Prevent recursion

        float resistance = MapSliderToResistance(R1Slider, value, invertR1);
        Debug.Log($"[Slider] R1 raw={value:F3} mappedResistance={resistance:F3}");
        if (manager != null)
            manager.UpdateResistanceAndRecalculate(0, resistance);
    }

    public void OnR2Changed(float value)
    {
        if (isSyncing) return;

        float resistance = MapSliderToResistance(R2Slider, value, invertR2);
        Debug.Log($"[Slider] R2 raw={value:F3} mappedResistance={resistance:F3}");
        if (manager != null)
            manager.UpdateResistanceAndRecalculate(1, resistance);
    }

    public void OnR3Changed(float value)
    {
        if (isSyncing) return;

        float resistance = MapSliderToResistance(R3Slider, value, invertR3);
        Debug.Log($"[Slider] R3 raw={value:F3} mappedResistance={resistance:F3}");
        if (manager != null)
            manager.UpdateResistanceAndRecalculate(2, resistance);
    }

    
}
