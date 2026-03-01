using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private float moveSpeed;
    private float destroyZThreshold;
    private SpaceshipAgent _agent; 

    // Update this signature to accept the agent
    public void Initialize(float speed, float destroyZ, SpaceshipAgent agent)
    {
        moveSpeed = speed;
        destroyZThreshold = destroyZ;
        _agent = agent; // Store the local agent!
    }

    void Update()
    {
        // Move backwards (relative to world or player direction)
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);

        // CLEANUP LOGIC:
        if (transform.position.z < destroyZThreshold)
        {
            // NEW: If this object is a star, penalize the agent before deleting it
            if (gameObject.CompareTag("Star") && _agent != null)
            {
                _agent.MissedStar();
            }
            
            Destroy(gameObject);
        }
    }
}