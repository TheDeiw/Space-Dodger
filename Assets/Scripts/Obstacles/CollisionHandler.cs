using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] GameObject destroyVFX;
    
    // Reference to the agent so we can give rewards/penalties for laser hits
    private SpaceshipAgent _agent;

    void Start()
    {
        // Find the agent in the scene (there should be one on the player)
        _agent = FindAnyObjectByType<SpaceshipAgent>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("PlayerLaser"))
        {
            Instantiate(destroyVFX, transform.position, Quaternion.identity);

            // Reward or punish based on what the laser destroyed
            if (_agent != null)
            {
                if (gameObject.CompareTag("Obstacle"))
                {
                    _agent.RewardObstacleDestroyed();
                }
                else if (gameObject.CompareTag("Star"))
                {
                    _agent.PenalizeStarDestroyed();
                }
            }

            Destroy(gameObject);
            Destroy(other.gameObject);
        }
    }
}
