using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class StageViewTrigger : MonoBehaviour
{
    [SerializeField] private StageViewController stageView;
    [SerializeField, Min(0)] private int viewIndex;
    [SerializeField] private bool oneShot;
    [SerializeField] private RespawnPoint introFallCheckpoint;

    private bool used;
    private RespawnPoint checkpoint;

    private void OnEnable()
    {
        checkpoint = GetComponent<RespawnPoint>();
        if (checkpoint != null)
            checkpoint.Reached += ActivateView;
    }

    private void OnDisable()
    {
        if (checkpoint != null)
            checkpoint.Reached -= ActivateView;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (checkpoint == null)
            ActivateView(other.GetComponentInParent<PlayerController>());
    }

    private void ActivateView(PlayerController player)
    {
        if (used || stageView == null || player == null || !stageView.ControlsPlayer(player))
            return;

        if (introFallCheckpoint != null && !player.SetRespawnPoint(introFallCheckpoint))
            return;

        used = oneShot || introFallCheckpoint != null;
        stageView.SetView(viewIndex);
    }
}
