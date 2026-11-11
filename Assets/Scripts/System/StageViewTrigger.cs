using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class StageViewTrigger : MonoBehaviour
{
    [SerializeField] private StageViewController stageView;
    [SerializeField, Min(0)] private int viewIndex;
    [SerializeField] private bool oneShot;
    [SerializeField] private RespawnPoint introFallCheckpoint;

    private bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || stageView == null)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !stageView.ControlsPlayer(player))
            return;

        if (introFallCheckpoint != null && !player.SetRespawnPoint(introFallCheckpoint))
            return;

        used = oneShot || introFallCheckpoint != null;
        stageView.SetView(viewIndex);
    }
}
