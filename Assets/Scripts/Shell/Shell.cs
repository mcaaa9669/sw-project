using UnityEngine;

// �÷��̾ ���� �ڸ��� ���� shell
// ��� ������ ���� PlayerController�� �Ʒ� �Լ� �� �ϳ��� ȣ��
[RequireComponent(typeof(Rigidbody2D))]
public class Shell : MonoBehaviour
{
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // ����: ���� ��ġ�� ����
    public void OnSpikeDeath()
    {
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