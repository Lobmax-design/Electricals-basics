using UnityEngine;

// NOTE: Renamed to ParallelCircuitManager to match your desired class name
public class ParallelCircuitAnimator : MonoBehaviour
{
    // === START OF SOLVER/MANAGER FIELDS (Used by the overall GO) ===

    // ... (All existing fields for the solver, RData, etc., go here) ...

    // === END OF SOLVER/MANAGER FIELDS ===

    // ====================================================================
    // NOTE: This class is used for TWO purposes: 
    // 1. Solver (on ParallelSolver GO)
    // 2. Animator (on the TotalCurrent_Electron, Branch Data Holders, and Prefab)
    // The fields below are ONLY used when attached to an electron or data holder.
    // ====================================================================

    [Header("Split/Merge Trigger")]
    public JunctionSplitter SplitterReference;
    public bool ShouldLoop = false;

    [Header("Animation Path")]
    public Transform[] pathPoints;
    public float animationSpeedFactor = 5.0f;
    public float arrivalTolerance = 0.001f;

    private float currentFlow = 0f;
    private float moveSpeed;
    public int currentPointIndex = 0; // Made public for external tracking if needed
    private bool hasStarted = false;

    // --- Core Animation Loop ---
    void Update()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;

        // Initial setup: snap to the start point
        if (!hasStarted)
        {
            if (pathPoints.Length > 0)
            {
                transform.position = pathPoints[0].position;
            }
            hasStarted = true;
            return;
        }

        moveSpeed = currentFlow * animationSpeedFactor;
        if (moveSpeed < 0.01f) return;

        // Define the current target point
        Transform targetPoint = pathPoints[currentPointIndex];

        // --- Move the Electron ---
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint.position,
            moveSpeed * Time.deltaTime
        );

        // --- Check if Target Reached ---
        if (Vector3.Distance(transform.position, targetPoint.position) < arrivalTolerance)
        {
            // Check if we are AT the last point in the path (e.g., index 2 of an array of length 3)
            if (currentPointIndex >= pathPoints.Length - 1)
            {
                // This is the end of the path segment.

                if (ShouldLoop)
                {
                    // 1. TOTAL CURRENT PATH: Trigger the Split and go back to start
                    if (SplitterReference != null)
                    {
                        SplitterReference.TriggerSplitAndSpawn();
                    }

                    currentPointIndex = 0; // Reset index to loop back to battery
                    transform.position = pathPoints[0].position; // Snap position to start
                }
                else
                {
                    // 2. BRANCH PATH: Hit the merge point, destroy the electron
                    Destroy(gameObject, 0.01f); // Destroy itself shortly after reaching the merge point
                }
            }
            else
            {
                // Normal path traversal: Advance to the next point
                currentPointIndex++;
            }
        }
    }

    // Public method called by ParallelCircuitManager to set the flow (speed)
    public void SetCurrentFlow(float newCurrent)
    {
        currentFlow = newCurrent;
    }

    // Public method used by the JunctionSplitter to read the branch speed
    public float GetCurrentFlow()
    {
        return currentFlow;
    }

    // ... (You must include the rest of the ParallelCircuitManager solver code, 
    // including the UpdateResistanceAndRecalculate and CalculateCircuit methods here) ...
}
