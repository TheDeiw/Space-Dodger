using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private float moveSpeed;
    private float destroyZThreshold;

    public float MoveSpeed => moveSpeed;

    public void Initialize(float speed, float destroyZ)
    {
        moveSpeed = speed;
        destroyZThreshold = destroyZ;
    }

    void Update()
    {
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);

        if (transform.position.z < destroyZThreshold)
        {
            Destroy(gameObject);
        }
    }
}