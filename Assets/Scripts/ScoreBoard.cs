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
    
    public void IncreaseScore(int amount)
    {
        _score += amount;
        UpdateScoreText();
    }
    
    private void UpdateScoreText()
    {
        scoreText.text = _score.ToString();
    }
}
