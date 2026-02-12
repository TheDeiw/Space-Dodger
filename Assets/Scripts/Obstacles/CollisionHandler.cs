using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] GameObject destroyVFX;

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("PlayerLaser"))
        {
            Instantiate(destroyVFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
            Destroy(other.gameObject);
        }
    }
}
