using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] GameObject destroyVFX;

    // Reference to the agent so we can give rewards/penalties for laser hits
    // Set by ObstacleSpawner (for obstacles/stars) or PlayerWeapon (for lasers)
    private SpaceshipAgent _agent;

    public void SetShooter(SpaceshipAgent agent)
    {
        _agent = agent;
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
