using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using System.Linq;

public class SpaceshipAgent : Agent
{
    private PlayerMovement _playerMovement;
    private PlayerWeapon _playerWeapon;
    private Vector3 _startingPosition;
    private BehaviorParameters _behaviorParameters;

    /// <summary>
    /// Returns true when the agent is in training mode (Default behavior connected to Python).
    /// Scoreboard and other UI elements should be disabled during training.
    /// </summary>
    public bool IsTraining
    {
        get
        {
            if (_behaviorParameters == null) return false;
            return _behaviorParameters.BehaviorType == BehaviorType.Default;
        }
    }

    [Header("Observation Settings")]
    [Tooltip("How many nearest stars to observe")]
    [SerializeField] private int maxStarsToObserve = 5;
    [Tooltip("How many nearest obstacles to observe")]
    [SerializeField] private int maxObstaclesToObserve = 5;
    [Tooltip("Maximum detection range for observations")]
    [SerializeField] private float detectionRange = 80f;

    [Header("Reward Settings")]
    [SerializeField] private float starCollectReward = 1.0f;
    [SerializeField] private float crashPenalty = -1.0f;
    [SerializeField] private float obstacleDestroyedReward = 0.3f;
    [SerializeField] private float starDestroyedPenalty = -0.3f;
    [SerializeField] private float survivalRewardPerStep = 0.001f;
    [SerializeField] private float movingTowardsStarReward = 0.005f;
    [SerializeField] private float shootPenalty = -0.002f;
    [SerializeField] private float missedStarPenalty = -0.05f;


    [Header("Episode Settings")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;

    // Clamp ranges — synced from PlayerMovement in Initialize()
    private float _xRange;
    private float _yRange;

    public override void Initialize()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerWeapon = GetComponent<PlayerWeapon>();
        _behaviorParameters = GetComponent<BehaviorParameters>();
        _startingPosition = transform.localPosition;

        // Sync observation normalization with actual movement clamp ranges
        _xRange = _playerMovement.XClampedRange;
        _yRange = _playerMovement.YClampedRange;
    }

    public override void OnEpisodeBegin()
    {
        // Reset position and velocity
        transform.localPosition = _startingPosition;
        _playerMovement.SetMovementInput(Vector2.zero);

        // Clear all spawned obstacles/stars from previous episode
        if (obstacleSpawner != null)
        {
            obstacleSpawner.ClearAllObstacles();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: Horizontal (0 = Idle, 1 = Left, 2 = Right)
        int moveX = actions.DiscreteActions[0];
        // Branch 1: Vertical (0 = Idle, 1 = Up, 2 = Down)
        int moveY = actions.DiscreteActions[1];
        // Branch 2: Shooting (0 = Don't Shoot, 1 = Shoot)
        int shoot = actions.DiscreteActions[2];

        // Translate the discrete actions into a Vector2 for your movement script
        float xInput = 0f;
        if (moveX == 1) xInput = -1f;
        else if (moveX == 2) xInput = 1f;

        float yInput = 0f;
        if (moveY == 1) yInput = 1f;
        else if (moveY == 2) yInput = -1f;

        // Feed the input to the body
        _playerMovement.SetMovementInput(new Vector2(xInput, yInput));

        // Pull the trigger if the AI decides to shoot
        if (shoot == 1)
        {
            _playerWeapon.FireWeapon();
            AddReward(shootPenalty);
        }

        // Small survival reward to encourage staying alive
        AddReward(survivalRewardPerStep);

        // Reward for moving towards the nearest star
        GameObject nearestStar = FindNearestWithTag("Star");
        if (nearestStar != null)
        {
            Vector3 dirToStar = (nearestStar.transform.position - transform.position).normalized;
            Vector3 moveDir = new Vector3(xInput, yInput, 0f).normalized;
            if (moveDir.magnitude > 0.1f)
            {
                float dot = Vector3.Dot(moveDir, dirToStar);
                AddReward(dot * movingTowardsStarReward);
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === 1. Player's own normalized position (2 values) ===
        sensor.AddObservation(transform.localPosition.x / _xRange);
        sensor.AddObservation(transform.localPosition.y / _yRange);

        // === 2. Nearest Stars — relative position (x, y, z) for each (maxStarsToObserve * 3 values) ===
        List<GameObject> stars = GetNearestObjectsWithTag("Star", maxStarsToObserve);
        for (int i = 0; i < maxStarsToObserve; i++)
        {
            if (i < stars.Count)
            {
                Vector3 relativePos = stars[i].transform.position - transform.position;
                sensor.AddObservation(relativePos.x / detectionRange);
                sensor.AddObservation(relativePos.y / detectionRange);
                sensor.AddObservation(relativePos.z / detectionRange);
            }
            else
            {
                // No star detected in this slot — fill with zeros
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        // === 3. Nearest Obstacles — relative position (x, y, z) for each (maxObstaclesToObserve * 3 values) ===
        List<GameObject> obstacles = GetNearestObjectsWithTag("Obstacle", maxObstaclesToObserve);
        for (int i = 0; i < maxObstaclesToObserve; i++)
        {
            if (i < obstacles.Count)
            {
                Vector3 relativePos = obstacles[i].transform.position - transform.position;
                sensor.AddObservation(relativePos.x / detectionRange);
                sensor.AddObservation(relativePos.y / detectionRange);
                sensor.AddObservation(relativePos.z / detectionRange);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        // Total observations: 2 + (5*3) + (5*3) = 32
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // This maps your keyboard inputs to the ML-Agent branches so you can test it manually
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Horizontal
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 2;
        else discreteActionsOut[0] = 0;

        // Vertical
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) discreteActionsOut[1] = 2;
        else discreteActionsOut[1] = 0;

        // Shoot
        if (Input.GetKey(KeyCode.Space)) discreteActionsOut[2] = 1;
        else discreteActionsOut[2] = 0;
    }

    // ==================== REWARD METHODS ====================
    // Called by PlayerCollisionHandler when player collects a star
    public void RewardStar()
    {
        AddReward(starCollectReward);
    }

    // Called by PlayerCollisionHandler when player hits an obstacle
    public void Crash()
    {
        AddReward(crashPenalty);
        EndEpisode();
    }

    // Called by CollisionHandler when the laser destroys an obstacle
    public void RewardObstacleDestroyed()
    {
        AddReward(obstacleDestroyedReward);
    }

    // Called by CollisionHandler when the laser accidentally destroys a star
    public void PenalizeStarDestroyed()
    {
        AddReward(starDestroyedPenalty);
    }

    public void MissedStar()
    {
        AddReward(missedStarPenalty);
    }

    // ==================== HELPER METHODS ====================
    private List<GameObject> GetNearestObjectsWithTag(string objectTag, int maxCount)
    {
        // Query ONLY objects spawned by the local spawner (no cross-environment contamination)
        if (obstacleSpawner == null) return new List<GameObject>();

        return obstacleSpawner.SpawnedObjects
            .Where(obj => obj != null && obj.CompareTag(objectTag))
            .OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position))
            .Where(obj => Vector3.Distance(transform.position, obj.transform.position) <= detectionRange)
            .Take(maxCount)
            .ToList();
    }

    private GameObject FindNearestWithTag(string objectTag)
    {
        // Local-only query with detection range cap
        if (obstacleSpawner == null) return null;

        GameObject nearest = null;
        float minDist = detectionRange;

        foreach (GameObject obj in obstacleSpawner.SpawnedObjects)
        {
            if (obj == null || !obj.CompareTag(objectTag)) continue;
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = obj;
            }
        }
        return nearest;
    }
}