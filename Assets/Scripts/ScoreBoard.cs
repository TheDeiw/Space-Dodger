using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class ScoreBoard : MonoBehaviour
{
    private int _score=0;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private SpaceshipAgent agent;
    
    void Start()
    {
        _score = 0;
        UpdateScoreText();
        
        // Hide the scoreboard UI during training
        if (agent != null && agent.IsTraining)
        {
            gameObject.SetActive(false);
        }
    }
    
    public void IncreaseScore()
    {
        _score ++;
        UpdateScoreText();
    }
    
    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + _score.ToString();
    }
    
    public  void ResetScore()
    {
        _score = 0;
        UpdateScoreText();
    }
}
