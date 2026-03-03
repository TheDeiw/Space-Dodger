using UnityEngine;
using TMPro;
using Unity.MLAgents.Policies;

public class ScoreBoard : MonoBehaviour
{
    private float _elapsedTime = 0f;
    private bool _isRunning = false;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private SpaceshipAgent agent;

    void Start()
    {
        // Check BehaviorParameters directly — works even if agent.Initialize() hasn't fired yet
        bool isTraining = false;
        if (agent != null)
        {
            var bp = agent.GetComponent<BehaviorParameters>();
            isTraining = bp != null && bp.BehaviorType == BehaviorType.Default;
        }

        // Completely disable the scoreboard during training to save performance
        if (isTraining)
        {
            _isRunning = false;
            gameObject.SetActive(false);
            return;
        }

        _elapsedTime = 0f;
        _isRunning = true;
        UpdateTimerText();
    }

    void Update()
    {
        if (!_isRunning) return;

        _elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        scoreText.text = "Time: " + _elapsedTime.ToString("F1") + "s";
    }

    /// <summary>
    /// Resets the survival timer back to 0. Called by PlayerCollisionHandler on crash.
    /// </summary>
    public void ResetTimer()
    {
        _elapsedTime = 0f;
        UpdateTimerText();
    }
}
