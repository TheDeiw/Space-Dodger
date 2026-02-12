using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject[] obstaclePrefabs;
    [SerializeField] Transform spawnOriginPlane; // Drag your floor/plane here
    [SerializeField] float spawnInterval = 1f;

    [Header("Size Modifiers")]
    [SerializeField] float minSize = 0.5f;
    [SerializeField] float maxSize = 2.0f;

    [Header("Speed Modifiers")]
    [SerializeField] float minSpeed = 10f;
    [SerializeField] float maxSpeed = 30f;

    [Header("Cleanup Settings")]
    [Tooltip("The Z position behind the player where objects get deleted")]
    [SerializeField] float destroyZPos = -20f;

    private bool isSpawning = true;
    private Bounds spawnBounds;

    void Start()
    {
        // Get the boundaries of the plane you assigned
        if (spawnOriginPlane != null)
        {
            // If the plane has a Renderer or Collider, we get its bounds
            Renderer planeRenderer = spawnOriginPlane.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                spawnBounds = planeRenderer.bounds;
            }
            else
            {
                Debug.LogError("Spawn Plane needs a Renderer or Collider to calculate bounds!");
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0) return;

        // 1. Pick a random object
        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefabToSpawn = obstaclePrefabs[randomIndex];

        // 2. Calculate Random Position based on the Plane's bounds
        // We use the Plane's X width, but we keep the Z fixed at the far end of the plane
        float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);
        
        // If you want them to spawn vertically relative to the plane, usually Y is slightly offset
        // Adjust this if your plane is flat on Y=0
        Vector3 spawnPos = new Vector3(randomX, randomY, spawnBounds.max.z);
        
        // 3. Instantiate with random angle
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject instance = Instantiate(prefabToSpawn, spawnPos, randomRotation);

        // 4. Apply Random Scale
        float randomScale = Random.Range(minSize, maxSize);
        instance.transform.localScale = Vector3.one * randomScale;

        // 5. Apply Random Speed & Initialize
        float randomSpeed = Random.Range(minSpeed, maxSpeed);
        
        // Ensure the prefab has the Mover script
        ObstacleMover mover = instance.GetComponent<ObstacleMover>();
        if (mover == null) mover = instance.AddComponent<ObstacleMover>();
        
        mover.Initialize(randomSpeed, destroyZPos);
    }
}