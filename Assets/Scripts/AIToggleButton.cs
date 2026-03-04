using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents.Policies;
using TMPro;

/// <summary>
/// Toggles the SpaceshipAgent between Inference Only (AI) and Heuristic Only (Player) modes.
/// Attach to a UI Button. Assign the SpaceshipAgent and the Button's TMP_Text in Inspector.
/// </summary>
public class AIToggleButton : MonoBehaviour
{
    [SerializeField] private SpaceshipAgent spaceshipAgent;
    [SerializeField] private TMP_Text buttonLabel;

    private BehaviorParameters _behaviorParameters;
    private PlayerMovement _playerMovement;
    private bool _isAIMode;

    void Start()
    {
        _behaviorParameters = spaceshipAgent.GetComponent<BehaviorParameters>();
        _playerMovement = spaceshipAgent.GetComponent<PlayerMovement>();

        // Read initial state from what's set in the Inspector
        _isAIMode = _behaviorParameters.BehaviorType == BehaviorType.InferenceOnly;
        UpdateLabel();

        // Hook up button click
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(Toggle);
        }
    }

    public void Toggle()
    {
        _isAIMode = !_isAIMode;

        if (_isAIMode)
        {
            _behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
            _playerMovement.SetAgentControlled(true);
        }
        else
        {
            _behaviorParameters.BehaviorType = BehaviorType.HeuristicOnly;
            _playerMovement.SetAgentControlled(false);
            // Stop any residual AI movement
            _playerMovement.SetMovementInput(Vector2.zero);
        }

        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (buttonLabel != null)
        {
            buttonLabel.text = _isAIMode ? "AI Mode ON" : "AI Mode OFF";
        }
    }
}
