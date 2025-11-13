using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Platform Movement Settings")]
    [SerializeField] private Transform[] waypoints = new Transform[3];
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutSine;
    [SerializeField] private bool startMovingOnStart = true;
    
    [Header("Platform Settings")]
    [SerializeField] private bool carryObjects = true;
    
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private LTDescr currentTween;
    private bool movingForward = true;

    
    void Start()
    {
        if (startMovingOnStart && ValidateWaypoints())
        {
            StartMoving();
        }
    }
    
    private bool ValidateWaypoints()
    {
        if (waypoints.Length != 3)
        {
            Debug.LogError($"MovingPlatform: Exactly 3 waypoints required, found {waypoints.Length}");
            return false;
        }
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                Debug.LogError($"MovingPlatform: Waypoint {i} is null");
                return false;
            }
        }
        
        return true;
    }
    
    public void StartMoving()
    {
        if (!ValidateWaypoints() || isMoving) return;
        
        // Set initial position to first waypoint
        transform.position = waypoints[0].position;
        currentWaypointIndex = 0;
        
        MoveToNextWaypoint();
    }
    
    public void StopMoving()
    {
        if (currentTween != null)
        {
            LeanTween.cancel(currentTween.uniqueId);
        }
        isMoving = false;
    }
    
    private void MoveToNextWaypoint()
    {
        if (!ValidateWaypoints()) return;
        
        isMoving = true;
        
        // Determine next waypoint based on current position in cycle
        int nextIndex = GetNextWaypointIndex();
        Vector3 targetPosition = waypoints[nextIndex].position;
        
        // Move platform using LeanTween
        currentTween = LeanTween.move(gameObject, targetPosition, moveDuration)
            .setEase(easeType)
            .setOnComplete(() => {
                currentWaypointIndex = nextIndex;
                isMoving = false;
                
                // Continue moving to next waypoint
                MoveToNextWaypoint();
            });
    }

    private int GetNextWaypointIndex()
    {
        // Handle the ping-pong pattern: 0 -> 1 -> 2 -> 1 -> 0 -> repeat

        if (movingForward)
        {
            // Moving from 0 to 2
            if (currentWaypointIndex < 2)
            {
                return currentWaypointIndex + 1;
            }
            else
            {
                // Reached the end (index 2), now go backward
                movingForward = false;
                return currentWaypointIndex - 1;
            }
        }
        else
        {
            // Moving from 2 back to 0
            if (currentWaypointIndex > 0)
            {
                return currentWaypointIndex - 1;
            }
            else
            {
                // Reached the start (index 0), now go forward again
                movingForward = true;
                return currentWaypointIndex + 1;
            }
        }
    }


    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length != 3) return;
        
        // Draw waypoints
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                
                // Draw waypoint numbers
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(waypoints[i].position + Vector3.up * 0.5f, $"Point {i + 1}");
                #endif
            }
        }
        
        // Draw movement path
        Gizmos.color = Color.green;
        if (waypoints[0] != null && waypoints[1] != null)
            Gizmos.DrawLine(waypoints[0].position, waypoints[1].position);
        if (waypoints[1] != null && waypoints[2] != null)
            Gizmos.DrawLine(waypoints[1].position, waypoints[2].position);
        if (waypoints[2] != null && waypoints[1] != null)
            Gizmos.DrawLine(waypoints[2].position, waypoints[1].position);
        
        // Draw current platform position
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
