using UnityEngine;
using System.Collections.Generic;

public class JunctionSplitter : MonoBehaviour
{
    // Reuse the data structure from the Parallel Circuit Manager
    [System.Serializable]
    public class BranchData
    {
        // Now holds the reference to the animator (path/speed template)
        public ParallelCircuitAnimator animatorReference;
        [HideInInspector] public float BranchCurrent;
    }

    [Header("Electron Prefab")]
    public GameObject electronPrefab;

    [Header("Total Flow Data")]
    // This list holds the data objects for R1, R2, R3 branches
    public List<BranchData> allBranches;

    // Total current flow rate (used for probabilistic spawning)
    private float totalI_entering = 0f;

    // Reference to the main Parallel Circuit Manager to get I values
    private ParallelCircuitManager manager;

    private void Start()
    {
        // Attempt to find the manager in the scene
        manager = FindObjectOfType<ParallelCircuitManager>();
        if (manager == null)
        {
            Debug.LogError("JunctionSplitter requires ParallelCircuitManager to be in the scene.");
            enabled = false;
            return;
        }

        // Populate initial current data from the manager
        // This is necessary because the animators only receive speed after CalculateCircuit runs.
        UpdateBranchCurrentsFromManager();
    }

    // Called by the ParallelCircuitManager when R values change
    public void UpdateBranchCurrentsFromManager()
    {
        if (manager == null) return;

        totalI_entering = 0f;

        // 1. Read the current data calculated by the ParallelCircuitManager
        for (int i = 0; i < allBranches.Count; i++)
        {
            // Assuming the index aligns between this list and the Manager's ParallelResistors list
            // We read the calculated current from the manager's list
            float branchI = manager.ParallelResistors[i].BranchCurrent;
            allBranches[i].BranchCurrent = branchI;
            totalI_entering += branchI;

            // 2. ALSO update the speed on the data holder itself
            if (allBranches[i].animatorReference != null)
            {
                allBranches[i].animatorReference.SetCurrentFlow(branchI);
            }
        }
    }

    // --- CRITICAL METHOD: Called by the Total Current Electron when it hits the junction ---
    // This method executes the split.
    public void TriggerSplitAndSpawn()
    {
        if (allBranches.Count == 0 || totalI_entering < 0.001f) return;

        // Perform the probabilistic routing based on current ratio
        float randomValue = UnityEngine.Random.value;
        float cumulativeRatio = 0f;

        for (int i = 0; i < allBranches.Count; i++)
        {
            BranchData branch = allBranches[i];

            // Calculate probability: I_branch / I_total
            float pathProbability = branch.BranchCurrent / totalI_entering;
            cumulativeRatio += pathProbability;

            if (randomValue <= cumulativeRatio)
            {
                // Spawn a new electron and send it down this path
                SpawnElectronOnPath(branch.animatorReference, branch.BranchCurrent);
                break;
            }
        }
    }

    void SpawnElectronOnPath(ParallelCircuitAnimator pathAnimatorTemplate, float branchCurrent)
    {
        // 1. Instantiate the electron at the junction point (this GameObject's position)
        GameObject electron = Instantiate(electronPrefab, transform.position, Quaternion.identity);

        // 2. Get reference to the newly spawned electron's animator
        ParallelCircuitAnimator electronAnimator = electron.GetComponent<ParallelCircuitAnimator>();

        if (electronAnimator != null)
        {
            // 3. Set the electron's path and speed based on the data holder template
            electronAnimator.pathPoints = pathAnimatorTemplate.pathPoints; // Copy the path array
            electronAnimator.SetCurrentFlow(branchCurrent); // Set speed (In)

            // Crucial: Ensure the spawned electron knows it must destroy itself
            electronAnimator.ShouldLoop = false;
        }
    }
}

