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
            // Skip VFX during training for performance
            if (_agent == null || !_agent.IsTraining)
            {
                Instantiate(destroyVFX, transform.position, Quaternion.identity);
            }

            // [DISABLED] Shooting rewards/penalties removed — agent cannot shoot in dodge-only mode
            // if (_agent != null)
            // {
            //     if (gameObject.CompareTag("Obstacle"))
            //     {
            //         _agent.RewardObstacleDestroyed();
            //     }
            //     else if (gameObject.CompareTag("Star"))
            //     {
            //         _agent.PenalizeStarDestroyed();
            //     }
            // }

            Destroy(gameObject);
            Destroy(other.gameObject);
        }
    }
}
