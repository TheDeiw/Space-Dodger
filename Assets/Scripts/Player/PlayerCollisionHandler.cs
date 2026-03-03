using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private ScoreBoard scoreBoard;
    [SerializeField] private GameObject explosionVFX;

    [SerializeField] private SpaceshipAgent agent;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            bool isTraining = agent != null && agent.IsTraining;

            if (!isTraining && scoreBoard != null) scoreBoard.ResetTimer();
            if (!isTraining && explosionVFX != null)
            {
                Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
            }

            // During training: don't destroy obstacle — it keeps flying for other areas/realism
            // During gameplay: destroy as usual
            if (!isTraining)
            {
                Destroy(other.gameObject);
            }

            if (agent != null) agent.Crash();
        }
    }
}
