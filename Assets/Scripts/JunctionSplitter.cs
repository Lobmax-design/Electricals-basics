using UnityEngine;
using System.Collections.Generic;

public class JunctionSplitter : MonoBehaviour
{
    [Header("Electron Prefab")]
    public GameObject electronPrefab;

    [Header("Output Paths")]
    // The animators for the branches (R1, R2, R3 branches)
    public List<CurrentAnimator> branchAnimators;

    [Header("Timing")]
    public float currentFlowRate = 1.0f; // Total I passed from the ParallelCircuitManager
    public float timeBetweenSpawns = 0.1f;
    private float spawnTimer;

    // NOTE: We assume CurrentAnimator.currentFlow is now PUBLIC or accessed via a public getter.
    // We will use the public property from the next section for safety.

    private void Update()
    {
        // The total number of electrons spawned per second is proportional to I_total.
        // If currentFlowRate is 0, timeBetweenSpawns will be very large, preventing rapid spawning.

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0)
        {
            SpawnAndDistributeElectrons();
            // Reset the timer, scaled inversely by the total current flow rate
            // Higher currentFlowRate means less time between spawns (faster spawning)
            // Use 1.0f / currentFlowRate, or a clamped value if currentFlowRate is very high.
            // Using currentFlowRate for timing ensures spawning frequency reflects I_total.
            timeBetweenSpawns = 1.0f / currentFlowRate;
            spawnTimer = timeBetweenSpawns;
        }
    }

    public void SetTotalFlowRate(float totalI)
    {
        // Total flow rate for the animation (passed from ParallelCircuitManager.cs)
        // Ensure minimum flow to avoid division by zero.
        currentFlowRate = Mathf.Max(totalI, 0.01f);
    }

    void SpawnAndDistributeElectrons()
    {
        // 1. Determine the path ratios (Based on current I)

        // Sum all currents to get the total I entering the junction
        float totalI_entering = 0f;
        foreach (var animator in branchAnimators)
        {
            // FIX: Access the current flow using a public property (assumed in next section)
            totalI_entering += animator.GetCurrentFlow();
        }

        if (totalI_entering < 0.001f) return; // If current is zero, don't spawn.

        // 2. Spawn and assign electrons one at a time

        // FIX: Explicitly use UnityEngine.Random to resolve ambiguity
        float randomValue = UnityEngine.Random.value; // A random number between 0 and 1

        float cumulativeRatio = 0f;

        // Iterate through all possible branches to determine which path the electron should take
        for (int i = 0; i < branchAnimators.Count; i++)
        {
            CurrentAnimator animator = branchAnimators[i];

            // Calculate the probability ratio for this branch: I_branch / I_total
            // FIX: Access the current flow using a public property (assumed in next section)
            float branchCurrent = animator.GetCurrentFlow();
            float pathProbability = branchCurrent / totalI_entering;
            cumulativeRatio += pathProbability;

            // If the random value falls into this path's probability range
            if (randomValue <= cumulativeRatio)
            {
                SpawnElectronOnPath(animator, branchCurrent); // Pass branch current to spawn
                break; // Stop iteration once path is chosen
            }
        }
    }

    void SpawnElectronOnPath(CurrentAnimator pathAnimator, float branchCurrent)
    {
        // 1. Instantiate the electron at the junction point
        GameObject electron = Instantiate(electronPrefab, transform.position, Quaternion.identity);

        // 2. Get a reference to the newly spawned electron's CurrentAnimator script
        CurrentAnimator electronAnimator = electron.GetComponent<CurrentAnimator>();

        if (electronAnimator != null)
        {
            // 3. Set the electron's path and speed based on the target branch's animator
            electronAnimator.pathPoints = pathAnimator.pathPoints; // Clone the path structure

            // FIX: Use the calculated branch current
            electronAnimator.SetCurrentFlow(branchCurrent);
        }

        // 4. Implement self-destruction after the loop is complete to prevent scene clutter
        // For example: electron.AddComponent<CleanupScript>().lifetime = 5f;
    }
}


