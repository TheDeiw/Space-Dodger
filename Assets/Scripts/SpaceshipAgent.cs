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
    // [DISABLED] Shooting removed to simplify the task — agent only dodges obstacles now
    // private PlayerWeapon _playerWeapon;
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
    // [DISABLED] Stars removed from observations — task simplified to obstacle avoidance only
    // [Tooltip("How many nearest stars to observe")]
    // [SerializeField] private int maxStarsToObserve = 5;
    [Tooltip("How many nearest obstacles to observe")]
    [SerializeField] private int maxObstaclesToObserve = 5;
    [Tooltip("Maximum detection range for observations")]
    [SerializeField] private float detectionRange = 80f;
    // [DISABLED] Speed observation removed — all obstacles now have a single fixed speed
    // [Tooltip("Max possible object speed, used to normalize speed observations")]
    // [SerializeField] private float maxObjectSpeed = 400f;

    [Header("Reward Settings")]
    // [DISABLED] Star-related rewards removed — no stars in simplified dodging task
    // [SerializeField] private float starCollectReward = 5.0f;
    [SerializeField] private float crashPenalty = -1.0f;
    // [DISABLED] Shooting rewards removed — agent cannot shoot anymore
    // [SerializeField] private float obstacleDestroyedReward = 0.5f;
    // [SerializeField] private float starDestroyedPenalty = -0.3f;
    [SerializeField] private float survivalRewardPerStep = 0.01f;
    // [DISABLED] Shooting penalty removed — no shooting in dodge-only mode
    // [SerializeField] private float shootPenalty = -0.001f;
    // [DISABLED] No stars to miss in dodge-only mode
    // [SerializeField] private float missedStarPenalty = -0.05f;

    [Header("Proximity Shaping")]
    // [DISABLED] Star proximity shaping removed — no stars in simplified task
    // [Tooltip("Small reward for being aligned with the nearest star on X/Y plane")]
    // [SerializeField] private float proximityRewardScale = 0.005f;
    [Tooltip("Reward for being far from nearest obstacle on X/Y plane")]
    [SerializeField] private float dodgeRewardScale = 0.01f;
    // [DISABLED] Star distance tracking removed — not needed without stars
    // private float _previousXYDistToStar = float.MaxValue;

    [Header("Episode Settings")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;

    // Clamp ranges — synced from PlayerMovement in Initialize()
    private float _xRange;
    private float _yRange;

    public override void Initialize()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        // [DISABLED] Weapon not needed for dodge-only task
        // _playerWeapon = GetComponent<PlayerWeapon>();
        _behaviorParameters = GetComponent<BehaviorParameters>();
        _startingPosition = transform.localPosition;

        // Sync observation normalization with actual movement clamp ranges
        _xRange = _playerMovement.XClampedRange;
        _yRange = _playerMovement.YClampedRange;

        // Add DecisionRequester: decide every 3 steps, repeat last action between decisions
        if (GetComponent<DecisionRequester>() == null)
        {
            var dr = gameObject.AddComponent<DecisionRequester>();
            dr.DecisionPeriod = 3;
            dr.TakeActionsBetweenDecisions = true;
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset position and velocity
        transform.localPosition = _startingPosition;
        _playerMovement.SetMovementInput(Vector2.zero);

        // Clear all spawned obstacles from previous episode
        if (obstacleSpawner != null)
        {
            obstacleSpawner.ClearAllObstacles();
        }

        // [DISABLED] Star proximity tracker no longer needed
        // _previousXYDistToStar = float.MaxValue;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: Horizontal (0 = Idle, 1 = Left, 2 = Right)
        int moveX = actions.DiscreteActions[0];
        // Branch 1: Vertical (0 = Idle, 1 = Up, 2 = Down)
        int moveY = actions.DiscreteActions[1];
        // [DISABLED] Branch 2 (Shooting) removed — simplified to dodge-only task
        // int shoot = actions.DiscreteActions[2];

        // Translate the discrete actions into a Vector2 for your movement script
        float xInput = 0f;
        if (moveX == 1) xInput = -1f;
        else if (moveX == 2) xInput = 1f;

        float yInput = 0f;
        if (moveY == 1) yInput = 1f;
        else if (moveY == 2) yInput = -1f;

        // Feed the input to the body
        _playerMovement.SetMovementInput(new Vector2(xInput, yInput));

        // [DISABLED] Shooting action removed for simplified training
        // if (shoot == 1)
        // {
        //     _playerWeapon.FireWeapon();
        //     AddReward(shootPenalty);
        // }

        // Small survival reward to encourage staying alive
        AddReward(survivalRewardPerStep);

        // [DISABLED] Star proximity shaping removed — no stars in dodge-only mode
        // GameObject nearestStar = FindNearestWithTag("Star");
        // if (nearestStar != null)
        // {
        //     float dx = nearestStar.transform.position.x - transform.position.x;
        //     float dy = nearestStar.transform.position.y - transform.position.y;
        //     float currentXYDist = Mathf.Sqrt(dx * dx + dy * dy);
        //     if (_previousXYDistToStar < float.MaxValue)
        //     {
        //         float delta = _previousXYDistToStar - currentXYDist;
        //         AddReward(delta * proximityRewardScale);
        //     }
        //     _previousXYDistToStar = currentXYDist;
        // }
        // else
        // {
        //     _previousXYDistToStar = float.MaxValue;
        // }

        // === Dodge shaping: small reward for keeping X/Y distance from nearest obstacle ===
        GameObject nearestObstacle = FindNearestWithTag("Obstacle");
        if (nearestObstacle != null)
        {
            float obsDx = nearestObstacle.transform.position.x - transform.position.x;
            float obsDy = nearestObstacle.transform.position.y - transform.position.y;
            float obsXYDist = Mathf.Sqrt(obsDx * obsDx + obsDy * obsDy);
            // Only reward when obstacle is close on Z (within 30 units ahead)
            float obsZ = nearestObstacle.transform.position.z - transform.position.z;
            if (obsZ > 0f && obsZ < 30f)
            {
                // Normalize: reward scales from 0 (on top of obstacle) to dodgeRewardScale (far away)
                float normalizedDist = Mathf.Clamp01(obsXYDist / 20f);
                AddReward(normalizedDist * dodgeRewardScale);
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // === 1. Player's own normalized position (2 values) ===
        sensor.AddObservation(transform.localPosition.x / _xRange);
        sensor.AddObservation(transform.localPosition.y / _yRange);

        // [DISABLED] Star observations removed — task simplified to obstacle avoidance only
        // List<GameObject> stars = GetNearestObjectsWithTag("Star", maxStarsToObserve);
        // for (int i = 0; i < maxStarsToObserve; i++)
        // {
        //     if (i < stars.Count)
        //     {
        //         Vector3 relativePos = stars[i].transform.position - transform.position;
        //         sensor.AddObservation(relativePos.x / detectionRange);
        //         sensor.AddObservation(relativePos.y / detectionRange);
        //         sensor.AddObservation(relativePos.z / detectionRange);
        //         ObstacleMover mover = stars[i].GetComponent<ObstacleMover>();
        //         sensor.AddObservation(mover != null ? mover.MoveSpeed / maxObjectSpeed : 0f);
        //     }
        //     else
        //     {
        //         sensor.AddObservation(0f);
        //         sensor.AddObservation(0f);
        //         sensor.AddObservation(0f);
        //         sensor.AddObservation(0f);
        //     }
        // }

        // === 2. Nearest Obstacles AHEAD — relative pos (x,y,z) only, no speed (maxObstaclesToObserve * 3 values) ===
        // Speed observation removed — all obstacles share the same fixed speed now
        List<GameObject> obstacles = GetNearestObstaclesAhead(maxObstaclesToObserve);
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

        // Total observations: 2 + (5*3) = 17
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Horizontal
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[0] = 2;
        else discreteActionsOut[0] = 0;

        // Vertical
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) discreteActionsOut[1] = 2;
        else discreteActionsOut[1] = 0;

        // [DISABLED] Shoot branch removed — only 2 branches now (horizontal + vertical)
        // if (Input.GetKey(KeyCode.Space)) discreteActionsOut[2] = 1;
        // else discreteActionsOut[2] = 0;
    }

    // ==================== REWARD METHODS ====================

    // [DISABLED] Star collection removed — dodge-only task
    // public void RewardStar()
    // {
    //     AddReward(starCollectReward);
    // }

    // Called by PlayerCollisionHandler when player hits an obstacle
    public void Crash()
    {
        AddReward(crashPenalty);
        EndEpisode();
    }

    // [DISABLED] Shooting rewards removed — agent cannot shoot anymore
    // public void RewardObstacleDestroyed()
    // {
    //     AddReward(obstacleDestroyedReward);
    // }

    // [DISABLED] No stars to accidentally destroy
    // public void PenalizeStarDestroyed()
    // {
    //     AddReward(starDestroyedPenalty);
    // }

    // [DISABLED] No stars to miss
    // public void MissedStar()
    // {
    //     AddReward(missedStarPenalty);
    // }

    // ==================== HELPER METHODS ====================
    
    /// <summary>
    /// Returns the nearest obstacles that are AHEAD of the agent (relative z > 0).
    /// Obstacles behind the agent are ignored — they pose no threat.
    /// Sorted by distance so closest threats come first.
    /// </summary>
    private List<GameObject> GetNearestObstaclesAhead(int maxCount)
    {
        if (obstacleSpawner == null) return new List<GameObject>();

        return obstacleSpawner.SpawnedObjects
            .Where(obj => obj != null && obj.CompareTag("Obstacle"))
            .Where(obj => (obj.transform.position.z - transform.position.z) > 0f) // only ahead
            .Where(obj => Vector3.Distance(transform.position, obj.transform.position) <= detectionRange)
            .OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position))
            .Take(maxCount)
            .ToList();
    }

    // [DISABLED] Generic tag query — replaced by GetNearestObstaclesAhead for observations
    // private List<GameObject> GetNearestObjectsWithTag(string objectTag, int maxCount)
    // {
    //     if (obstacleSpawner == null) return new List<GameObject>();
    //     return obstacleSpawner.SpawnedObjects
    //         .Where(obj => obj != null && obj.CompareTag(objectTag))
    //         .OrderBy(obj => Vector3.Distance(transform.position, obj.transform.position))
    //         .Where(obj => Vector3.Distance(transform.position, obj.transform.position) <= detectionRange)
    //         .Take(maxCount)
    //         .ToList();
    // }

    private GameObject FindNearestWithTag(string objectTag)
    {
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