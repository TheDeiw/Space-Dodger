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
        
        if (other.CompareTag("Star"))
        {
            if (agent != null) agent.RewardStar();
            
            if (!isTraining) scoreBoard.IncreaseScore();
            Destroy(other.gameObject);
            Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
        }
        else if (other.CompareTag("Obstacle"))
        {
            if (!isTraining) scoreBoard.ResetScore();
            Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
            Destroy(other.gameObject);
            
            if (agent != null) agent.Crash();
            //ResetPlayerPosition();
        }
    }

    void ResetPlayerPosition()
    {
        gameObject.transform.position = new Vector3(0f, 0f, 0f);
    }
}
