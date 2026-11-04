using UnityEngine;
using System.Collections.Generic;

// �÷��̾ ���� �ڸ��� ���� shell
// ��� ������ ���� PlayerController�� �Ʒ� �Լ� �� �ϳ��� ȣ��
[RequireComponent(typeof(Rigidbody2D))]
public class Shell : MonoBehaviour
{
    Rigidbody2D rb;
    [SerializeField, Min(0f)] float poisonHorizontalDamping = 8f;
    [SerializeField, Min(0f)]
    float spikeEmbedDepth = 0.2f;

    readonly HashSet<Collider2D> poisonContacts = new();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PoisonTrigger>() != null)
            poisonContacts.Add(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        poisonContacts.Remove(other);
    }

    void FixedUpdate()
    {
        // 비활성화되거나 삭제된 독 영역은 제외
        poisonContacts.RemoveWhere(c =>
            c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        if (poisonContacts.Count == 0) return;
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 velocity = rb.linearVelocity;

        // 가로 속도만 감쇠. 세로 속도는 유지.
        velocity.x *= Mathf.Exp(
            -poisonHorizontalDamping * Time.fixedDeltaTime);

        rb.linearVelocity = velocity;
    }

    void OnDisable()
    {
        poisonContacts.Clear();
    }

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // ����: ���� ��ġ�� ����
    public void OnSpikeDeath()
    {
        // 가시 사망일 때만 조금 아래에 배치
        rb.position += Vector2.down * spikeEmbedDepth;
        Freeze();
    }

    // ������: ���� ��Ģ�� ���� �������ų� ���ο��� �̲�����
    public void OnLaserDeath()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;                 // �������� �ʰ� �̲�������
    }

    // ��: 3�� �� �׶��� ��ġ�� ����
    public void OnPoisonDeath()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;
    }

    void Freeze()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
    }
}