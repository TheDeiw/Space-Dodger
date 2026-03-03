using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] GameObject destroyVFX;

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("PlayerLaser"))
        {
            if (destroyVFX != null)
            {
                Instantiate(destroyVFX, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
            Destroy(other.gameObject);
        }
    }
}
