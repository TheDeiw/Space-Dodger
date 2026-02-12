using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class ScoreBoard : MonoBehaviour
{
    private int _score=0;
    [SerializeField] private TMP_Text scoreText;
    
    void Start()
    {
        _score = 0;
        UpdateScoreText();
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
