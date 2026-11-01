using System;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public static event Action<Coin> Collected;

    public float spinSpeed = 180f;

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collected?.Invoke(this);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(1);
        Destroy(gameObject);
    }
}
