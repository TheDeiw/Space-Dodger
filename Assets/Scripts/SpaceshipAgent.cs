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
    [Tooltip("Survival is the primary objective — increased weight")]
    [SerializeField] private float survivalRewardPerStep = 0.005f;
    [Tooltip("Bonus reward for surviving the full episode")]
    [SerializeField] private float episodeCompletionBonus = 2.0f;

    [Header("Active Evasion Reward")]
    [Tooltip("Bonus per step for moving when close sector shows danger. Prevents idle center-camping.")]
    [SerializeField] private float activeDodgeBonus = 0.003f;
    [Tooltip("Minimum close-sector danger value (0-1) to trigger the evasion bonus")]
    [SerializeField] private float dangerThresholdForBonus = 0.3f;

    [Header("Episode Settings")]
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [Tooltip("Max steps before episode ends successfully (0 = unlimited)")]
    [SerializeField] private int maxStepsPerEpisode = 3000;

    [Header("Sector Observations")]
    [Tooltip("Far sector grid: planning-ahead distance")]
    [SerializeField] private float farObservationDistance = 50f;
    [Tooltip("Close sector grid: immediate danger distance")]
    [SerializeField] private float closeObservationDistance = 15f;

    // Clamp ranges — synced from PlayerMovement in Initialize()
    private float _xRange;
    private float _yRange;

    // Sector boundaries (derived from clamp ranges)
    private float _sectorXWidth;
    private float _sectorYHeight;

    // Track previous action for observation
    private float _lastActionX;
    private float _lastActionY;

    // 3x3 sector danger grids — far (planning) + close (immediate), reused to avoid GC
    private float[] _sectorDanger = new float[9];
    private float[] _sectorClosestZ = new float[9];
    private float[] _sectorDangerClose = new float[9];
    private float[] _sectorClosestZClose = new float[9];

    public override void Initialize()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _behaviorParameters = GetComponent<BehaviorParameters>();
        _startingPosition = transform.localPosition;
        PlayerWeapon weaponplayer = GetComponent<PlayerWeapon>();
        if (weaponplayer != null)
        {
            weaponplayer.SetDisabled(true); // Disable weapon during training to prevent accidental firing
        }

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

        // 1) Survival is the PRIMARY reward — agent must learn to stay alive above all else
        AddReward(survivalRewardPerStep);

        // 2) Active evasion bonus: reward the agent for MOVING when an obstacle is close.
        //    This breaks the "stand still in center" local optimum — the agent must actively dodge.
        bool isMoving = (xInput != 0f || yInput != 0f);
        if (isMoving)
        {
            float maxCloseDanger = 0f;
            foreach (float d in _sectorDangerClose)
                maxCloseDanger = Mathf.Max(maxCloseDanger, d);

            if (maxCloseDanger > dangerThresholdForBonus)
            {
                AddReward(activeDodgeBonus * maxCloseDanger);
            }
        }

        // 3) End episode with bonus if agent survived the full duration
        if (maxStepsPerEpisode > 0 && StepCount >= maxStepsPerEpisode)
        {
            AddReward(episodeCompletionBonus);
            EndEpisode();
        }
    }

    // ===================== OBSERVATIONS =====================
    // Vector obs: 2 (pos) + 2 (action) + 9 (far sector grid) + 9 (close sector grid) = 22
    // Plus any RayPerceptionSensor3D attached in Inspector (auto-detected)

    public override void CollectObservations(VectorSensor sensor)
    {
        // === 1) Player's own normalized position (2 values) ===
        sensor.AddObservation(transform.localPosition.x / _xRange);
        sensor.AddObservation(transform.localPosition.y / _yRange);

        // === 2) Last action taken (2 values) — helps predict momentum ===
        sensor.AddObservation(_lastActionX);
        sensor.AddObservation(_lastActionY);

        // === 3) FAR 3×3 Sector danger grid (9 values, up to farObservationDistance) ===
        // Gives planning-ahead info: what's coming in the next ~50 units
        AddSectorDangerObservations(sensor, farObservationDistance, _sectorDanger, _sectorClosestZ);

        // === 4) CLOSE 3×3 Sector danger grid (9 values, up to closeObservationDistance) ===
        // Gives immediate danger: what's RIGHT in front — react NOW
        AddSectorDangerObservations(sensor, closeObservationDistance, _sectorDangerClose, _sectorClosestZClose);
    }

    /// <summary>
    /// 3×3 sector grid observation. Divides the area ahead into sectors:
    ///   [6:TL] [7:TC] [8:TR]   (Y > +threshold)
    ///   [3:ML] [4:MC] [5:MR]   (Y ≈ 0)
    ///   [0:BL] [1:BC] [2:BR]   (Y < -threshold)
    /// Each value = 1 - (closestZ / maxDist). Higher = closer = more danger.
    /// Call with different maxDist for far (planning) and close (immediate) grids.
    /// </summary>
    private void AddSectorDangerObservations(VectorSensor sensor, float maxDist,
        float[] danger, float[] closestZ)
    {
        // Reset
        for (int i = 0; i < 9; i++)
        {
            danger[i] = 0f;
            closestZ[i] = maxDist;
        }

        if (obstacleSpawner != null)
        {
            foreach (GameObject obs in obstacleSpawner.SpawnedObjects)
            {
                if (obs == null) continue;
                Vector3 rel = obs.transform.position - transform.position;

                // Only consider obstacles AHEAD (positive Z relative to player)
                if (rel.z <= 0f || rel.z > maxDist) continue;

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
                if (rel.z < closestZ[idx])
                {
                    closestZ[idx] = rel.z;
                }
            }
        }

        // Convert to danger values and add to sensor
        for (int i = 0; i < 9; i++)
        {
            if (closestZ[i] < maxDist)
            {
                danger[i] = 1f - (closestZ[i] / maxDist);
            }
            sensor.AddObservation(danger[i]);
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