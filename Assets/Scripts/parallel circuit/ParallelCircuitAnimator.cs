using UnityEngine;
using System.Collections.Generic;

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
    public Transform[] branchPathPoints;
    public int[] branchResistorIndices;
}

public class ParallelCircuitAnimator : MonoBehaviour
{
    [Header("Animation Path")]
    public List<PathPoint> pathPoints;
    public float animationSpeedFactor = 5.0f;
    public float arrivalTolerance = 0.001f;

    [Header("Branch Settings")]
    public GameObject electronPrefab;
    public bool ShouldLoop = false;

    private ParallelCircuitManager manager;
    private float currentFlow = 0f;
    private float moveSpeed = 0f;
    private int currentPointIndex = 0;
    private bool initialized = false;

    [HideInInspector] public ParallelCircuitAnimator templateReference;
    private bool isBranch = false;
    private Transform[] branchPath;
    private int branchIndex = 0;
    private int resistorIndex = -1;

    private bool waitingForMerge = false;
    private int branchesExpected = 0;
    private int branchesArrived = 0;
    private int mergePointIndex = -1;
    private Transform mergePointTransform = null;

    private List<Renderer> renderersCache;
    private List<GameObject> activeBranches = new List<GameObject>(); // 🆕 Track active branches

    void Awake()
    {
        renderersCache = new List<Renderer>(GetComponentsInChildren<Renderer>());
        manager = FindObjectOfType<ParallelCircuitManager>();

        if (manager != null)
            manager.CurrentsUpdated += OnCurrentsUpdated;
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.CurrentsUpdated -= OnCurrentsUpdated;
    }

    void Start()
    {
        TryInitializePosition();
    }

    void Update()
    {
        if (isBranch)
            BranchPathUpdate();
        else
            MainPathUpdate();
    }

    public void SetCurrentFlow(float newCurrent)
    {
        currentFlow = newCurrent;
        moveSpeed = currentFlow * animationSpeedFactor;
    }

    private void OnCurrentsUpdated()
    {
        if (manager == null) return;

        if (isBranch)
        {
            if (resistorIndex >= 0 && resistorIndex < manager.ParallelResistors.Count)
            {
                float newCurrent = manager.ParallelResistors[resistorIndex].BranchCurrent;
                SetCurrentFlow(newCurrent);
            }
        }
        else
        {
            float totalI = 0f;
            foreach (var r in manager.ParallelResistors)
                totalI += r.BranchCurrent;
            SetCurrentFlow(totalI);
        }
    }

    private void MainPathUpdate()
    {
        if (waitingForMerge) return;
        if (pathPoints == null || pathPoints.Count == 0) return;
        if (currentPointIndex < 0 || currentPointIndex >= pathPoints.Count) return;

        var current = pathPoints[currentPointIndex];
        if (current == null || current.point == null) return;

        if (moveSpeed < 0.0001f) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            current.point.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, current.point.position) < arrivalTolerance)
        {
            if (current.type == PathPointType.Split && current.branchPathPoints != null && current.branchPathPoints.Length > 0)
            {
                mergePointIndex = FindNextMergeIndex(currentPointIndex);
                mergePointTransform = (mergePointIndex >= 0) ? pathPoints[mergePointIndex].point : null;
                StartSplitSession(current);
                return;
            }

            if (currentPointIndex < pathPoints.Count - 1)
                currentPointIndex++;
            else if (ShouldLoop)
                currentPointIndex = 0;
            else
                Destroy(gameObject);
        }
    }

    private void BranchPathUpdate()
    {
        if (branchPath == null || branchPath.Length == 0) return;
        if (branchIndex < 0 || branchIndex >= branchPath.Length) return;

        var target = branchPath[branchIndex];
        if (target == null) return;

        if (moveSpeed < 0.0001f) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < arrivalTolerance)
        {
            if (branchIndex < branchPath.Length - 1)
                branchIndex++;
            else
            {
                if (templateReference != null)
                    templateReference.NotifyBranchArrived(this);
                else
                {
                    var main = FindObjectOfType<ParallelCircuitAnimator>();
                    if (main != null && main != this)
                        main.NotifyBranchArrived(this);
                }
                Destroy(gameObject, 0.05f);
            }
        }
    }

    // 🧩 --- FIXED METHOD ---
    private void StartSplitSession(PathPoint splitPoint)
    {
        if (splitPoint == null || splitPoint.branchPathPoints == null || splitPoint.branchPathPoints.Length == 0)
            return;

        // 🧹 Destroy any previously active branch electrons before spawning new ones
        CleanupActiveBranches();

        int configuredBranches = splitPoint.branchPathPoints.Length;
        int validBranches = 0;

        for (int i = 0; i < configuredBranches; i++)
        {
            var root = splitPoint.branchPathPoints[i];
            if (root == null) continue;
            var pts = GetBranchPath(root);
            if (pts != null && pts.Length > 0) validBranches++;
        }

        if (validBranches == 0) return;

        branchesExpected = validBranches;
        branchesArrived = 0;
        waitingForMerge = true;

        SetRenderersEnabled(false);

        bool useManagerMapping = (manager != null &&
                                  splitPoint.branchResistorIndices != null &&
                                  splitPoint.branchResistorIndices.Length == splitPoint.branchPathPoints.Length);

        for (int i = 0; i < configuredBranches; i++)
        {
            var branchRoot = splitPoint.branchPathPoints[i];
            if (branchRoot == null) continue;

            var branchPts = GetBranchPath(branchRoot);
            if (branchPts == null || branchPts.Length == 0) continue;

            var branchElectron = Instantiate(electronPrefab, splitPoint.point.position, Quaternion.identity);
            activeBranches.Add(branchElectron); // 🧩 Track this instance

            var anim = branchElectron.GetComponent<ParallelCircuitAnimator>();
            if (anim == null)
            {
                Destroy(branchElectron);
                continue;
            }

            anim.isBranch = true;
            anim.branchPath = branchPts;
            anim.branchIndex = 0;
            anim.templateReference = this;

            if (useManagerMapping)
            {
                if (splitPoint.branchResistorIndices != null && i < splitPoint.branchResistorIndices.Length)
                    anim.resistorIndex = splitPoint.branchResistorIndices[i];
            }

            if (branchPts[0] != null)
                branchElectron.transform.position = branchPts[0].position;

            var branchRenderers = branchElectron.GetComponentsInChildren<Renderer>(true);
            foreach (var r in branchRenderers)
                if (r != null) r.enabled = true;

            float branchCurrent = 0f;
            if (useManagerMapping)
            {
                int resistorIdx = anim.resistorIndex;
                if (resistorIdx >= 0 && manager != null && resistorIdx < manager.ParallelResistors.Count)
                    branchCurrent = manager.ParallelResistors[resistorIdx].BranchCurrent;
                else
                    branchCurrent = (currentFlow > 0f) ? (currentFlow / branchesExpected) : 0f;
            }
            else
            {
                branchCurrent = (currentFlow > 0f) ? (currentFlow / branchesExpected) : 0f;
            }

            anim.SetCurrentFlow(branchCurrent);
        }
    }

    private void CleanupActiveBranches() // 🧹 New cleanup function
    {
        if (activeBranches == null || activeBranches.Count == 0) return;

        foreach (var obj in activeBranches)
        {
            if (obj != null)
                Destroy(obj);
        }
        activeBranches.Clear();
    }

    private void ResumeAfterMerge()
    {
        waitingForMerge = false;
        branchesExpected = 0;
        branchesArrived = 0;

        // 🧹 Clean up branch references after merging
        CleanupActiveBranches();

        if (mergePointTransform != null)
        {
            transform.position = mergePointTransform.position;
            currentPointIndex = mergePointIndex;
        }

        SetRenderersEnabled(true);
    }

    public void NotifyBranchArrived(ParallelCircuitAnimator branch)
    {
        if (!waitingForMerge) return;

        branchesArrived++;
        if (branch != null)
            Destroy(branch.gameObject, 0.05f);

        if (branchesArrived >= branchesExpected)
            ResumeAfterMerge();
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
        if (branchRoot.childCount == 0)
            pts.Add(branchRoot);
        else
            foreach (Transform child in branchRoot)
                pts.Add(child);
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

        foreach (var r in renderersCache)
            if (r != null) r.enabled = enabled;
    }

#if UNITY_EDITOR
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
