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
        if (!other.TryGetComponent<PlayerController>(out var player))
            return;

        if (player.IsDead || poisonCoroutine != null)
            return;

        poisonCoroutine = StartCoroutine(PoisonTimer(player));
    }

    private IEnumerator PoisonTimer(PlayerController player)
    {
        Debug.Log($"[독] 접촉! {deathDelay}초 후 사망 요청");

        yield return new WaitForSeconds(deathDelay);

        if (player != null && !player.IsDead)
        {
            Debug.Log("[독] 5초 경과 — 사망 요청 신호 발생");

            // 나중에 사망 처리 담당 코드가 이 이벤트를 구독
            OnPoisonDeathRequested?.Invoke(player.gameObject);
        }

        poisonCoroutine = null;
    }
}