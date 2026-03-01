using System.Collections;
using System.Collections.Generic;
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

    [Header("Agent Reference")]
    [SerializeField] private SpaceshipAgent localAgent;

    private bool isSpawning = true;
    private Bounds spawnBounds;

    // Track all spawned objects so we can clear them on episode reset
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // Public read-only accessor for SpaceshipAgent to query local objects
    public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;

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

        // Prune destroyed objects to prevent list bloat
        spawnedObjects.RemoveAll(obj => obj == null);

        // 1. Pick a random object
        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefabToSpawn = obstaclePrefabs[randomIndex];

        // 2. Calculate Random Position based on the Plane's bounds
        float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);

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

        mover.Initialize(randomSpeed, destroyZPos, localAgent);

        // Initialize the CollisionHandler with the local agent reference
        CollisionHandler collisionHandler = instance.GetComponent<CollisionHandler>();
        if (collisionHandler != null)
        {
            collisionHandler.SetShooter(localAgent);
        }

        // Track the spawned object
        spawnedObjects.Add(instance);
    }

    /// <summary>
    /// Destroys all spawned obstacles and stars. Called by SpaceshipAgent on episode reset.
    /// </summary>
    public void ClearAllObstacles()
    {
        // Clean up ONLY the objects spawned by THIS specific spawner
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        // Empty the list so it is fresh for the next episode
        spawnedObjects.Clear();

        // REMOVED: GameObject.FindGameObjectsWithTag() 
        // We never use global searches in parallel ML-Agents!
    }
}