using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private ScoreBoard scoreBoard;
    [SerializeField] private GameObject explosionVFX;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Star"))
        {
            scoreBoard.IncreaseScore();
            Destroy(other.gameObject);
            Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
        }
        else if (other.CompareTag("Obstacle"))
        {
            scoreBoard.ResetScore();
            Instantiate(explosionVFX, other.transform.position, other.transform.rotation);
            Destroy(other.gameObject);
            
            ResetPlayerPosition();
        }
    }

    void ResetPlayerPosition()
    {
        gameObject.transform.position = new Vector3(0f, 0f, 0f);
    }
}
