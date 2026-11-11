using UnityEngine;

// �÷��̾ �������� ��ȥ���� ����� �Ѱų� ��
[RequireComponent(typeof(Collider2D))]
public class SoulWorldZone : MonoBehaviour
{
    [SerializeField] bool enableSoulWorld = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.SetSoulWorldEnabled(enableSoulWorld);
    }
}