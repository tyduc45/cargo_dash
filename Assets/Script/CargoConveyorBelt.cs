using UnityEngine;

public class CargoConveyorBelt : MonoBehaviour
{
    public Vector2 conveyorDirection = Vector2.right; // Customize direction
    public float conveyorSpeed = 2f;

    private void OnCollisionStay2D(Collision2D other)
    {
        // Check if the object has a Cargo component
        Cargo cargo = other.gameObject.GetComponent<Cargo>();
        if (cargo == null) return;
        
        Rigidbody2D rb = other.rigidbody;
        if (rb != null)
        {
            // Preserve the Y velocity (for jumping/falling) and only modify X velocity
            Vector2 newVelocity = new Vector2(
                conveyorDirection.normalized.x * conveyorSpeed, 
                rb.linearVelocity.y
            );
            rb.linearVelocity = newVelocity;
        }
    }
}
