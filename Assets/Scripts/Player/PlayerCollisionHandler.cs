using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private ScoreBoard scoreBoard;
    [SerializeField] private GameObject explosionVFX;
    
    [SerializeField] private SpaceshipAgent agent;
    
    void OnTriggerEnter(Collider other)
    {
        bool isTraining = agent != null && agent.IsTraining;
        
        // [DISABLED] Star collection removed — dodge-only task, no stars spawned anymore
        // if (other.CompareTag("Star"))
        // {
        //     if (agent != null) agent.RewardStar();
        //     
        //     if (!isTraining) scoreBoard.IncreaseScore();
        //     Destroy(other.gameObject);
        //     if (!isTraining) Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
        // }
        // else
        if (other.CompareTag("Obstacle"))
        {
            if (!isTraining && scoreBoard != null) scoreBoard.ResetTimer();
            if (!isTraining) Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
            Destroy(other.gameObject);
            
            if (agent != null) agent.Crash();
        }
    }

    void ResetPlayerPosition()
    {
        gameObject.transform.position = new Vector3(0f, 0f, 0f);
    }
}
