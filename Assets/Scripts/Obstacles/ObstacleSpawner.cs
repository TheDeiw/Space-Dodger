using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject[] obstaclePrefabs;
    [SerializeField] Transform spawnOriginPlane;
    [SerializeField] float spawnInterval = 0.8f;

    [Header("Size Modifiers")]
    [SerializeField] float minSize = 1f;
    [SerializeField] float maxSize = 2f;

    [Header("Speed")]
    [SerializeField] float obstacleSpeed = 60f;

    [Header("Cleanup Settings")]
    [Tooltip("The Z position behind the player where objects get deleted")]
    [SerializeField] float destroyZPos = -20f;

    [Header("Environment")]
    [Tooltip("Parent transform for spawned obstacles (assign the Environment root for multi-area setups)")]
    [SerializeField] private Transform environmentRoot;

    private bool isSpawning = true;
    private Bounds spawnBounds;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;

    void Start()
    {
        if (spawnOriginPlane != null)
        {
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

        spawnedObjects.RemoveAll(obj => obj == null);

        int randomIndex = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefabToSpawn = obstaclePrefabs[randomIndex];

        float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);

        Vector3 spawnPos = new Vector3(randomX, randomY, spawnBounds.max.z);

        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject instance = Instantiate(prefabToSpawn, spawnPos, randomRotation);

        // Parent to environment root for multi-area isolation
        if (environmentRoot != null)
        {
            instance.transform.SetParent(environmentRoot, true);
        }

        float randomScale = Random.Range(minSize, maxSize);
        instance.transform.localScale = Vector3.one * randomScale;

        ObstacleMover mover = instance.GetComponent<ObstacleMover>();
        if (mover == null) mover = instance.AddComponent<ObstacleMover>();

        mover.Initialize(obstacleSpeed, destroyZPos);

        spawnedObjects.Add(instance);
    }

    /// <summary>
    /// Destroys all spawned obstacles. Called by SpaceshipAgent on episode reset.
    /// </summary>
    public void ClearAllObstacles()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }
        spawnedObjects.Clear();
    }
}