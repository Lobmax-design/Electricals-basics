using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Animator that:
/// - follows a main path of PathPoint (Normal / Split / Merge),
/// - at a Split point spawns branch electrons and pauses (hides) the main electron,
/// - waits for all branches to report arrival at the Merge, then resumes as a single electron.
/// </summary>
public enum PathPointType
{
    Normal,
    Split,
    Merge
}

[System.Serializable]
public class PathPoint
{
    public Transform point;
    public PathPointType type;

    // Each element is a root whose children are the branch waypoints
    // (Inspector will enforce exactly 3 entries for a Split)
    public Transform[] branchPathPoints;

    // Map each branch to a ParallelResistors index (length must match branchPathPoints when Split)
    public int[] branchResistorIndices;
}

public class ParallelCircuitAnimator : MonoBehaviour
{
    [Header("Animation Path")]
    public List<PathPoint> pathPoints; // main path including Split and Merge points
    public float animationSpeedFactor = 5.0f;
    public float arrivalTolerance = 0.001f;

    [Header("Branch Settings")]
    public GameObject electronPrefab; // prefab must have ParallelCircuitAnimator attached
    public bool ShouldLoop = false;

    // optional runtime link to manager for realistic branch currents
    private ParallelCircuitManager manager;

    // runtime state
    [HideInInspector] public ParallelCircuitAnimator templateReference; // set on spawned branches
    private float currentFlow = 0f;
    private float moveSpeed = 0f;
    private int currentPointIndex = 0;
    private bool initialized = false;

    // branch instance state
    private bool isBranch = false;
    private Transform[] branchPath;
    private int branchIndex = 0;

    // merge coordination (main animator)
    private bool waitingForMerge = false;
    private int branchesExpected = 0;
    private int branchesArrived = 0;
    private int mergePointIndex = -1;
    private Transform mergePointTransform = null;
    private List<Renderer> renderersCache;

    void Awake()
    {
        renderersCache = new List<Renderer>(GetComponentsInChildren<Renderer>());
        manager = FindObjectOfType<ParallelCircuitManager>();
    }

    void Start()
    {
        TryInitializePosition();
    }

    void Update()
    {
        if (isBranch) BranchPathUpdate();
        else MainPathUpdate();
    }

    // Public API
    public void SetCurrentFlow(float newCurrent)
    {
        currentFlow = newCurrent;
        moveSpeed = currentFlow * animationSpeedFactor;
    }

    // Called by a branch instance when it reaches its branch end (merge)
    public void NotifyBranchArrived(ParallelCircuitAnimator branch)
    {
        if (!waitingForMerge) return;

        branchesArrived++;

        // destroy branch visual (safety)
        if (branch != null)
            Destroy(branch.gameObject, 0.01f);

        if (branchesArrived >= branchesExpected)
        {
            ResumeAfterMerge();
        }
    }

    // MAIN PATH: total electron
    private void MainPathUpdate()
    {
        if (waitingForMerge) return; // paused until branches return

        if (pathPoints == null || pathPoints.Count == 0) return;
        if (currentPointIndex < 0 || currentPointIndex >= pathPoints.Count) return;

        var current = pathPoints[currentPointIndex];
        if (current == null || current.point == null) return;

        moveSpeed = currentFlow * animationSpeedFactor;
        if (moveSpeed < 0.0001f) return;

        transform.position = Vector3.MoveTowards(transform.position, current.point.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, current.point.position) < arrivalTolerance)
        {
            if (current.type == PathPointType.Split && current.branchPathPoints != null && current.branchPathPoints.Length > 0)
            {
                mergePointIndex = FindNextMergeIndex(currentPointIndex);
                mergePointTransform = (mergePointIndex >= 0) ? pathPoints[mergePointIndex].point : null;
                StartSplitSession(current);
                return;
            }

            // advance
            if (currentPointIndex < pathPoints.Count - 1)
                currentPointIndex++;
            else if (ShouldLoop)
                currentPointIndex = 0;
            else
                Destroy(gameObject);
        }
    }

    // BRANCH behavior for spawned electrons
    private void BranchPathUpdate()
    {
        if (branchPath == null || branchPath.Length == 0) return;
        if (branchIndex < 0 || branchIndex >= branchPath.Length) return;

        var target = branchPath[branchIndex];
        if (target == null) return;

        moveSpeed = currentFlow * animationSpeedFactor;
        if (moveSpeed < 0.0001f) return;

        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < arrivalTolerance)
        {
            if (branchIndex < branchPath.Length - 1)
            {
                branchIndex++;
            }
            else
            {
                // notify main animator and allow it to destroy this branch
                if (templateReference != null)
                    templateReference.NotifyBranchArrived(this);
                else
                {
                    // best-effort fallback: find any main animator
                    var main = FindObjectOfType<ParallelCircuitAnimator>();
                    if (main != null && main != this)
                        main.NotifyBranchArrived(this);
                }

                // safety destroy if not cleaned up
                Destroy(gameObject, 0.05f);
            }
        }
    }

    // Start split: spawn branch electrons and pause/hide main
    private void StartSplitSession(PathPoint splitPoint)
    {
        if (splitPoint == null || splitPoint.branchPathPoints == null || splitPoint.branchPathPoints.Length == 0)
        {
            Debug.LogWarning($"[ParallelCircuitAnimator] Split misconfigured on '{gameObject.name}'.");
            return;
        }

        int configuredBranches = splitPoint.branchPathPoints.Length;

        // First pass: count valid branches (non-null root and with at least one child waypoint)
        int validBranches = 0;
        for (int i = 0; i < configuredBranches; i++)
        {
            var root = splitPoint.branchPathPoints[i];
            if (root == null) continue;
            var pts = GetBranchPath(root);
            if (pts != null && pts.Length > 0) validBranches++;
        }

        if (validBranches == 0)
        {
            Debug.LogWarning($"[ParallelCircuitAnimator] No valid branch waypoint paths found on Split for '{gameObject.name}'. Skipping split.");
            return;
        }

        // Prepare merge coordination only if we will spawn at least one branch
        branchesExpected = validBranches;
        branchesArrived = 0;
        waitingForMerge = true;

        // Hide main only when we have branches
        SetRenderersEnabled(false);

        // If manager has branch currents, attempt to use them via the mapping in PathPoint.branchResistorIndices
        bool useManagerMapping = (manager != null &&
                                  splitPoint.branchResistorIndices != null &&
                                  splitPoint.branchResistorIndices.Length == splitPoint.branchPathPoints.Length);

        // Spawn branches (skip invalid ones)
        for (int i = 0; i < configuredBranches; i++)
        {
            var branchRoot = splitPoint.branchPathPoints[i];
            if (branchRoot == null)
            {
                Debug.LogWarning($"[ParallelCircuitAnimator] Branch root {i} is null on '{gameObject.name}'. Skipping.");
                continue;
            }

            var branchPts = GetBranchPath(branchRoot);
            if (branchPts == null || branchPts.Length == 0)
            {
                Debug.LogWarning($"[ParallelCircuitAnimator] Branch {i} has no child waypoints on '{gameObject.name}'. Skipping.");
                continue;
            }

            var branchElectron = Instantiate(electronPrefab, splitPoint.point.position, Quaternion.identity);
            var anim = branchElectron.GetComponent<ParallelCircuitAnimator>();
            if (anim == null)
            {
                Debug.LogWarning("[ParallelCircuitAnimator] electronPrefab missing ParallelCircuitAnimator.");
                Destroy(branchElectron);
                // decrement expected because we counted valid roots based on waypoints, but guard just in case
                branchesExpected = Mathf.Max(0, branchesExpected - 1);
                continue;
            }

            anim.isBranch = true;
            anim.branchPath = branchPts;
            anim.branchIndex = 0;
            anim.templateReference = this;

            // place branch at its first waypoint so it doesn't immediately appear to be "missing"
            if (branchPts[0] != null)
                branchElectron.transform.position = branchPts[0].position;

            // ensure the instantiated object is active
            branchElectron.SetActive(true);

            // defensive: enable any Renderer components on the spawned branch (handles MeshRenderer=false on prefab)
            var branchRenderers = branchElectron.GetComponentsInChildren<Renderer>(true);
            if (branchRenderers != null && branchRenderers.Length > 0)
            {
                foreach (var r in branchRenderers)
                {
                    if (r == null) continue;
                    r.enabled = true;
                }
            }

            // determine branch current: try manager mapping first, else equal split fallback
            float branchCurrent = 0f;
            if (useManagerMapping)
            {
                int resistorIndex = -1;
                if (splitPoint.branchResistorIndices != null && i < splitPoint.branchResistorIndices.Length)
                    resistorIndex = splitPoint.branchResistorIndices[i];

                if (resistorIndex >= 0 && manager != null && resistorIndex < manager.ParallelResistors.Count)
                {
                    branchCurrent = manager.ParallelResistors[resistorIndex].BranchCurrent;
                }
                else
                {
                    // invalid mapping -> fallback
                    branchCurrent = (currentFlow > 0f) ? (currentFlow / branchesExpected) : 0f;
                }
            }
            else
            {
                branchCurrent = (currentFlow > 0f) ? (currentFlow / branchesExpected) : 0f;
            }

            anim.SetCurrentFlow(branchCurrent);
            Debug.Log($"[ParallelCircuitAnimator] Spawned branch {i} for '{gameObject.name}' with current={branchCurrent:F3} A and {branchPts.Length} waypoints.");
        }

        // Safety: if for some reason no branch was successfully instantiated, undo waiting state
        if (branchesExpected == 0)
        {
            waitingForMerge = false;
            SetRenderersEnabled(true);
            Debug.LogWarning($"[ParallelCircuitAnimator] No branches were instantiated at Split on '{gameObject.name}'. Restoring main electron.");
        }
    }

    private void ResumeAfterMerge()
    {
        waitingForMerge = false;
        branchesExpected = 0;
        branchesArrived = 0;

        if (mergePointTransform != null)
        {
            transform.position = mergePointTransform.position;
            currentPointIndex = mergePointIndex;
        }

        SetRenderersEnabled(true);
    }

    private int FindNextMergeIndex(int startIndex)
    {
        if (pathPoints == null) return -1;
        for (int i = startIndex + 1; i < pathPoints.Count; i++)
        {
            if (pathPoints[i] != null && pathPoints[i].type == PathPointType.Merge)
                return i;
        }
        return -1;
    }

    private Transform[] GetBranchPath(Transform branchRoot)
    {
        if (branchRoot == null) return new Transform[0];

        var pts = new List<Transform>();

        // If the branch root has no children, treat the root itself as a single waypoint.
        // This avoids skipping splits when users assign a waypoint directly instead of a root container.
        if (branchRoot.childCount == 0)
        {
            pts.Add(branchRoot);
            Debug.Log($"[ParallelCircuitAnimator] Branch root '{branchRoot.name}' has no children — using the root as a single waypoint.");
        }
        else
        {
            foreach (Transform child in branchRoot)
                pts.Add(child);
        }

        return pts.ToArray();
    }

    private void TryInitializePosition()
    {
        if (initialized) return;

        if (isBranch)
        {
            if (branchPath != null && branchPath.Length > 0 && branchPath[0] != null)
                transform.position = branchPath[0].position;
        }
        else
        {
            if (pathPoints != null && pathPoints.Count > 0 && pathPoints[0] != null && pathPoints[0].point != null)
                transform.position = pathPoints[0].point.position;
        }

        initialized = true;
    }

    private void SetRenderersEnabled(bool enabled)
    {
        if (renderersCache == null || renderersCache.Count == 0)
            renderersCache = new List<Renderer>(GetComponentsInChildren<Renderer>());

        foreach (var r in renderersCache) if (r != null) r.enabled = enabled;
    }

#if UNITY_EDITOR
    // Ensure Split PathPoints have arrays sized to 3 for editor convenience
    void OnValidate()
    {
        if (pathPoints == null) return;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            var p = pathPoints[i];
            if (p == null) continue;
            if (p.type == PathPointType.Split)
            {
                if (p.branchPathPoints == null || p.branchPathPoints.Length != 3)
                    p.branchPathPoints = new Transform[3];
                if (p.branchResistorIndices == null || p.branchResistorIndices.Length != 3)
                    p.branchResistorIndices = new int[3];
            }
        }
    }
#endif
}





