using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Text scoreText;
    public int score;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshScore();
    }

    public void AddScore(int amount)
    {
        score += amount;
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (scoreText != null) scoreText.text = "Coins: " + score;
    }
}
