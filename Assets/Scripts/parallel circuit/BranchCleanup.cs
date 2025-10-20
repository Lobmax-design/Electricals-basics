using UnityEngine;

public class BranchCleanup : MonoBehaviour
{
    // The delay before destroying the object after the end of its path is detected.
    // This allows the particle to briefly overlap the merge point visually.
    public float destroyDelay = 0.1f;

    // Public method to be called by CurrentAnimator when the final waypoint is hit.
    public void DestroySelf()
    {
        // Debug.Log("Electron reached merge point and is destroying itself.");
        Destroy(gameObject, destroyDelay);
    }
}

