using System;
using System.Collections;
using UnityEngine;

public class PoisonTrigger : MonoBehaviour
{
    [SerializeField] private float deathDelay = 5f;

    public event Action<GameObject> OnPoisonDeathRequested;

    private Coroutine poisonCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (poisonCoroutine != null) return;

        Debug.Log("독 접촉! 5초 후 사망");

        poisonCoroutine = StartCoroutine(PoisonTimer(other.gameObject));
    }

    private IEnumerator PoisonTimer(GameObject player)
    {
        yield return new WaitForSeconds(deathDelay);

        poisonCoroutine = null;

        if (player != null)
        {
            Debug.Log("독으로 인한 사망 요청");
            OnPoisonDeathRequested?.Invoke(player);
        }
    }
}