using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class ScoreBoard : MonoBehaviour
{
    // [DISABLED] Score counter replaced with survival timer for dodge-only mode
    // private int _score = 0;
    private float _elapsedTime = 0f;
    private bool _isRunning = false;
    
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private SpaceshipAgent agent;
    
    void Start()
    {
        // Completely disable the scoreboard during training to save performance
        if (agent != null && agent.IsTraining)
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
        // Double-check: never run during training even if Start() missed it
        if (!_isRunning || (agent != null && agent.IsTraining)) return;
        
        _elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }
    
    // [DISABLED] Star score no longer used — dodge-only task
    // public void IncreaseScore()
    // {
    //     _score++;
    //     UpdateScoreText();
    // }
    
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
    
    // [DISABLED] Old score reset replaced by ResetTimer above
    // public void ResetScore()
    // {
    //     _score = 0;
    //     UpdateScoreText();
    // }
}
