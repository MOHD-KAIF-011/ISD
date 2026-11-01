using System.Collections.Generic;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public Transform player;
    public int initialCoins = 8;
    public Vector2 areaSize = new Vector2(17f, 17f);
    public float spawnHeight = 1f;
    public float minDistanceFromPlayer = 3f;

    private readonly List<Coin> activeCoins = new List<Coin>();

    private void OnEnable()
    {
        Coin.Collected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        Coin.Collected -= HandleCoinCollected;
    }

    private void Start()
    {
        for (int i = 0; i < initialCoins; i++) SpawnCoin();
    }

    private void HandleCoinCollected(Coin coin)
    {
        activeCoins.Remove(coin);
        SpawnCoin();
    }

    private void SpawnCoin()
    {
        if (coinPrefab == null) return;

        Vector3 position = GetRandomPosition();
        GameObject instance = Instantiate(coinPrefab, position, coinPrefab.transform.rotation, transform);
        Coin coin = instance.GetComponent<Coin>();
        if (coin != null) activeCoins.Add(coin);
    }

    private Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float x = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);
            Vector3 candidate = center + new Vector3(x, spawnHeight, z);

            if (player == null) return candidate;

            Vector3 flatCandidate = new Vector3(candidate.x, 0f, candidate.z);
            Vector3 flatPlayer = new Vector3(player.position.x, 0f, player.position.z);
            if (Vector3.Distance(flatCandidate, flatPlayer) >= minDistanceFromPlayer) return candidate;
        }

        return center + new Vector3(0f, spawnHeight, 0f);
    }
}
