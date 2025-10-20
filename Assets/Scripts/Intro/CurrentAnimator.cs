using UnityEngine;

public class CurrentAnimator : MonoBehaviour
{
    // --- Public Properties (Set in Inspector) ---
    [Header("Animation Path")]
    public Transform[] pathPoints;
    public float animationSpeedFactor = 0.5f;
    public float arrivalTolerance = 0.001f; // Tolerance for reaching a point

    // --- Private Variables ---
    private float currentFlow = 1.2f;
    private float moveSpeed;
    private int currentPointIndex = 0;
    private bool hasStarted = false; // Tracks initial placement

    void Update()
    {
        if (pathPoints.Length == 0) return;

        // Ensure the electron is placed at the start point once
        if (!hasStarted)
        {
            transform.position = pathPoints[0].position;
            hasStarted = true;
        }

        // Calculate the actual speed (ensuring it's updated with currentFlow)
        moveSpeed = currentFlow * animationSpeedFactor;

        // Stop moving if the calculated speed is effectively zero
        if (moveSpeed < 0.001f) return;

        // Define the target point
        Transform targetPoint = pathPoints[currentPointIndex];

        // --- Move the Electron ---
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint.position,
            moveSpeed * Time.deltaTime
        );

        // --- Check if the Electron Reached the Target Point (ROBUST CHECK) ---
        if (Vector3.Distance(transform.position, targetPoint.position) < arrivalTolerance)
        {
            // Move to the next point in the path (looping logic)
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
        }
    }

    // Public method called by CircuitManager to update speed
    public void SetCurrentFlow(float newCurrent)
    {
        // Update the flow value (which will be used to calculate moveSpeed in Update)
        currentFlow = newCurrent;
    }
}