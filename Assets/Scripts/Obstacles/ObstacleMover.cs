using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private float moveSpeed;
    private float destroyZThreshold;

    // This method is called by the Spawner immediately after creating this object
    public void Initialize(float speed, float destroyZ)
    {
        moveSpeed = speed;
        destroyZThreshold = destroyZ;
    }

    void Update()
    {
        // Move backwards (relative to world or player direction)
        // Vector3.back is (0, 0, -1)
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);

        // CLEANUP LOGIC:
        // If the object passes the threshold behind the player, destroy it.
        if (transform.position.z < destroyZThreshold)
        {
            Destroy(gameObject);
        }
    }
}