using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] GameObject destroyVFX;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        Instantiate(destroyVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
