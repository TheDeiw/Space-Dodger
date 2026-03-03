using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class SpaceshipAgent : Agent
{
    private PlayerMovement _playerMovement;
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

    [Header("Reward Settings")]
    [SerializeField] private float crashPenalty = -1.0f;
    [SerializeField] private float survivalRewardPerStep = 0.001f;
    [Tooltip("Bonus reward for surviving the full episode")]
    [SerializeField] private float episodeCompletionBonus = 3.0f;

    [Header("Anti-Corner Penalty")]
    [Tooltip("Fraction of range (0-1) that counts as edge zone")]
    [SerializeField] private float edgeZoneThreshold = 0.75f;
    [Tooltip("Penalty per step when the agent is in the edge/corner zone")]
    [SerializeField] private float edgePenaltyPerStep = -0.003f;
    [Tooltip("Small reward per step for staying near center (within this fraction of range)")]
    [SerializeField] private float centerZoneThreshold = 0.4f;
    [SerializeField] private float centerBonusPerStep = 0.0005f;

    [Header("Episode Settings")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [Tooltip("Max steps before episode ends successfully (0 = unlimited)")]
    [SerializeField] private int maxStepsPerEpisode = 3000;

    [Header("Sector Observations")]
    [Tooltip("Max observation distance — obstacles further than this are ignored")]
    [SerializeField] private float maxObservationDistance = 50f;

    // Clamp ranges — synced from PlayerMovement in Initialize()
    private float _xRange;
    private float _yRange;

    // Sector boundaries (derived from clamp ranges)
    private float _sectorXWidth;
    private float _sectorYHeight;

    // Track previous action for observation
    private float _lastActionX;
    private float _lastActionY;

    // 3x3 sector danger grid (reused each frame to avoid GC)
    private float[] _sectorDanger = new float[9];
    private float[] _sectorClosestZ = new float[9];

    public override void Initialize()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _behaviorParameters = GetComponent<BehaviorParameters>();
        _startingPosition = transform.localPosition;

        // Sync observation normalization with actual movement clamp ranges
        _xRange = _playerMovement.XClampedRange;
        _yRange = _playerMovement.YClampedRange;

        // Sector boundaries = 1/3 of play area each side
        _sectorXWidth = _xRange / 3f;
        _sectorYHeight = _yRange / 3f;

        // Block Input System from overriding agent controls during training
        if (IsTraining)
        {
            _playerMovement.SetAgentControlled(true);

            // Disable weapon during training to prevent accidental laser firing
            var weapon = GetComponent<PlayerWeapon>();
            if (weapon != null) weapon.SetDisabled(true);
        }

        // ===== SANITY CHECK: warn if RayPerceptionSensor is missing =====
        var raySensors = GetComponentsInChildren<RayPerceptionSensorComponent3D>();
        if (raySensors.Length == 0)
        {
            Debug.LogWarning(
                "[SpaceshipAgent] No RayPerceptionSensorComponent3D found! " +
                "Using only sector-based vector observations.");
        }
        else
        {
            Debug.Log($"[SpaceshipAgent] Found {raySensors.Length} RayPerceptionSensor(s) — OK.");
        }

        // Add DecisionRequester: decide every 2 steps for faster reactions
        var dr = GetComponent<DecisionRequester>();
        if (dr == null)
        {
            dr = gameObject.AddComponent<DecisionRequester>();
        }
        dr.DecisionPeriod = 2;
        dr.TakeActionsBetweenDecisions = true;
    }

    public override void OnEpisodeBegin()
    {
        // Reset position and velocity — obstacles keep flying, no clearing!
        transform.localPosition = _startingPosition;
        _playerMovement.SetMovementInput(Vector2.zero);
        _lastActionX = 0f;
        _lastActionY = 0f;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Branch 0: Horizontal (0 = Idle, 1 = Left, 2 = Right)
        int moveX = actions.DiscreteActions[0];
        // Branch 1: Vertical (0 = Idle, 1 = Up, 2 = Down)
        int moveY = actions.DiscreteActions[1];

        float xInput = 0f;
        if (moveX == 1) xInput = -1f;
        else if (moveX == 2) xInput = 1f;

        float yInput = 0f;
        if (moveY == 1) yInput = 1f;
        else if (moveY == 2) yInput = -1f;

        _lastActionX = xInput;
        _lastActionY = yInput;

        _playerMovement.SetMovementInput(new Vector2(xInput, yInput));

        // ==================== REWARD SHAPING ====================

        // 1) Small survival reward each step
        AddReward(survivalRewardPerStep);

        // 2) Edge/corner penalty — discourages camping at borders
        float xRatio = Mathf.Abs(transform.localPosition.x) / _xRange;
        float yRatio = Mathf.Abs(transform.localPosition.y) / _yRange;
        if (xRatio > edgeZoneThreshold || yRatio > edgeZoneThreshold)
        {
            AddReward(edgePenaltyPerStep);
        }
        // Small center bonus — being near center gives best dodging options
        else if (xRatio < centerZoneThreshold && yRatio < centerZoneThreshold)
        {
            AddReward(centerBonusPerStep);
        }

        // 3) End episode with bonus if agent survived the full duration
        if (maxStepsPerEpisode > 0 && StepCount >= maxStepsPerEpisode)
        {
            AddReward(episodeCompletionBonus);
            EndEpisode();
        }
    }

    // ===================== OBSERVATIONS =====================
    // Vector obs: 2 (pos) + 2 (action) + 9 (sector grid) = 13
    // Plus any RayPerceptionSensor3D attached in Inspector (auto-detected)

    public override void CollectObservations(VectorSensor sensor)
    {
        // === 1) Player's own normalized position (2 values) ===
        sensor.AddObservation(transform.localPosition.x / _xRange);
        sensor.AddObservation(transform.localPosition.y / _yRange);

        // === 2) Last action taken (2 values) — helps predict momentum ===
        sensor.AddObservation(_lastActionX);
        sensor.AddObservation(_lastActionY);

        // === 3) 3×3 Sector danger grid (9 values) ===
        // Divides the space AHEAD of the player into 9 sectors.
        // Each sector reports how close the nearest obstacle is (0=safe, 1=imminent).
        // Spatially consistent: sector 0 is always bottom-left, sector 8 is always top-right.
        AddSectorDangerObservations(sensor);
    }

    /// <summary>
    /// 3×3 sector grid observation. Divides the area ahead into sectors:
    ///   [6:TL] [7:TC] [8:TR]   (Y > +threshold)
    ///   [3:ML] [4:MC] [5:MR]   (Y ≈ 0)
    ///   [0:BL] [1:BC] [2:BR]   (Y < -threshold)
    /// Each value = 1 - (closestZ / maxDist). Higher = closer = more danger.
    /// </summary>
    private void AddSectorDangerObservations(VectorSensor sensor)
    {
        // Reset
        for (int i = 0; i < 9; i++)
        {
            _sectorDanger[i] = 0f;
            _sectorClosestZ[i] = maxObservationDistance;
        }

        if (obstacleSpawner != null)
        {
            foreach (GameObject obs in obstacleSpawner.SpawnedObjects)
            {
                if (obs == null) continue;
                Vector3 rel = obs.transform.position - transform.position;

                // Only consider obstacles AHEAD (positive Z relative to player)
                if (rel.z <= 0f || rel.z > maxObservationDistance) continue;

                // Determine X sector: 0=left, 1=center, 2=right
                int sx;
                if (rel.x < -_sectorXWidth) sx = 0;
                else if (rel.x > _sectorXWidth) sx = 2;
                else sx = 1;

                // Determine Y sector: 0=bottom, 1=middle, 2=top
                int sy;
                if (rel.y < -_sectorYHeight) sy = 0;
                else if (rel.y > _sectorYHeight) sy = 2;
                else sy = 1;

                int idx = sy * 3 + sx;

                // Keep the closest obstacle per sector
                if (rel.z < _sectorClosestZ[idx])
                {
                    _sectorClosestZ[idx] = rel.z;
                }
            }
        }

        // Convert to danger values and add to sensor
        for (int i = 0; i < 9; i++)
        {
            if (_sectorClosestZ[i] < maxObservationDistance)
            {
                _sectorDanger[i] = 1f - (_sectorClosestZ[i] / maxObservationDistance);
            }
            sensor.AddObservation(_sectorDanger[i]);
        }
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
    }

    // ==================== REWARD METHODS ====================

    /// <summary>
    /// Called by PlayerCollisionHandler when player hits an obstacle.
    /// </summary>
    public void Crash()
    {
        AddReward(crashPenalty);
        EndEpisode();
    }
}