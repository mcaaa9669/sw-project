using System;
using UnityEngine;

[DefaultExecutionOrder(110)]
[RequireComponent(typeof(Camera))]
public sealed class StageViewController : MonoBehaviour
{
    public enum ViewMode { Fixed, FollowPlayer, FollowCamera }

    [Serializable]
    public class View
    {
        public string name;
        public ViewMode cameraMode;
        public Vector2 cameraCenter;
        public bool followBelowHeight;
        public bool followAtOrAboveHeight;
        public float followHeight;
        [Min(0.01f)] public float followOrthographicSize = 5f;
        public bool enableSoulVignette;
        public bool maximizeSoulVignette;
        public bool enableSoulWorld;
        [Min(0f)] public float soulWorldTimeOverride;
        public RespawnPoint startWhenPastCheckpoint;
        public BoxCollider2D leftExitBarrier;
        [Min(0.01f)] public float orthographicSize;
        public ViewMode backgroundMode;
        public Vector2 backgroundCenter;
        [Min(0f)] public float transitionDuration;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Transform background;
    [SerializeField] private GameObject soulVignette;
    [SerializeField] private SoulWorldController soulWorldController;
    [SerializeField] private Transform[] zoomScaledLayers = Array.Empty<Transform>();
    [SerializeField] private View[] views;

    private int currentView = -1;
    private Camera viewCamera;
    private float startingSize;
    private float startingFrameSize;
    private float frameSize;
    private float sceneSize;
    private float referenceZoom;
    private Vector3[] originalLayerScales;
    private Vector2 cameraStart;
    private Vector2 backgroundStart;
    private float elapsed;
    private bool followingHeightOverride;

    public bool ControlsPlayer(PlayerController candidate) => candidate.transform == player;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        sceneSize = viewCamera.orthographicSize;
        referenceZoom = views != null && views.Length > 0 && views[0].orthographicSize > 0f
            ? views[0].orthographicSize : sceneSize;
        frameSize = referenceZoom;
        originalLayerScales = new Vector3[zoomScaledLayers.Length];
        for (int i = 0; i < zoomScaledLayers.Length; i++)
        {
            if (zoomScaledLayers[i] != null)
                originalLayerScales[i] = zoomScaledLayers[i].localScale;
        }
    }

    private void Start()
    {
        if (player != null && views != null)
        {
            for (int index = views.Length - 1; index > 0; index--)
            {
                RespawnPoint checkpoint = views[index].startWhenPastCheckpoint;
                if (checkpoint == null || player.position.x < checkpoint.transform.position.x)
                    continue;

                if (player.TryGetComponent(out PlayerController controller))
                    controller.SetRespawnPoint(checkpoint);
                SetView(index);
                return;
            }
        }

        SetView(0);
    }

    public void SetView(int index)
    {
        if (views == null || index < 0 || index >= views.Length || index <= currentView)
            return;

        bool firstView = currentView < 0;
        currentView = index;
        View view = views[index];
        followingHeightOverride = ShouldFollowAtHeight(view);
        BeginTransition();
        ActivateLeftExitBarrier(view);
        if (soulWorldController != null)
            soulWorldController.SetStageTimeOverride(view.soulWorldTimeOverride);
        if (view.enableSoulWorld && player != null && player.TryGetComponent(out PlayerController controller))
            controller.SetSoulWorldEnabled(true);
        if (soulVignette != null)
            soulVignette.SetActive(view.enableSoulVignette);
        if (soulVignette != null && soulVignette.activeInHierarchy &&
            soulVignette.TryGetComponent(out SoulWorldVignette vignette))
            vignette.SetMaximumStrength(view.maximizeSoulVignette);

        if (firstView || view.transitionDuration <= 0f)
        {
            elapsed = view.transitionDuration;
            UpdateView(1f);
        }
    }

    private bool ShouldFollowAtHeight(View view)
    {
        if (player == null)
            return false;

        float height = player.localPosition.y;
        return (view.followBelowHeight && height < view.followHeight) ||
            (view.followAtOrAboveHeight && height >= view.followHeight);
    }

    private void BeginTransition()
    {
        cameraStart = transform.position;
        startingSize = viewCamera.orthographicSize;
        startingFrameSize = frameSize;
        if (background != null)
            backgroundStart = background.position;
        elapsed = 0f;
    }

    private void ActivateLeftExitBarrier(View view)
    {
        BoxCollider2D barrier = view.leftExitBarrier;
        if (barrier == null)
            return;

        float size = view.orthographicSize > 0f ? view.orthographicSize : sceneSize;
        float leftEdge = view.cameraCenter.x - size * viewCamera.aspect;
        if (player != null && player.TryGetComponent(out Collider2D playerCollider))
            leftEdge = Mathf.Min(leftEdge, playerCollider.bounds.min.x - 0.01f);

        SetCenter(barrier.transform, new Vector2(leftEdge - barrier.size.x * 0.5f, view.cameraCenter.y));
        barrier.enabled = true;
    }

    private void LateUpdate()
    {
        if (currentView < 0)
            return;

        View view = views[currentView];
        bool follow = ShouldFollowAtHeight(view);
        if (followingHeightOverride != follow)
        {
            followingHeightOverride = follow;
            BeginTransition();
        }

        elapsed += Time.deltaTime;
        float duration = view.transitionDuration;
        float t = duration > 0f ? Mathf.SmoothStep(0f, 1f, elapsed / duration) : 1f;
        UpdateView(t);
    }

    private void UpdateView(float t)
    {
        View view = views[currentView];
        Vector2 cameraGoal = view.cameraCenter;
        if (followingHeightOverride)
            cameraGoal = player.position;
        else if (view.cameraMode == ViewMode.FollowPlayer && player != null)
            cameraGoal += (Vector2)player.position;
        SetCenter(transform, Vector2.Lerp(cameraStart, cameraGoal, t));
        float size = view.orthographicSize > 0f ? view.orthographicSize : sceneSize;
        float cameraSize = followingHeightOverride ? view.followOrthographicSize : size;
        viewCamera.orthographicSize = Mathf.Lerp(startingSize, cameraSize, t);
        frameSize = Mathf.Lerp(startingFrameSize, size, t);
        float zoomRatio = frameSize / referenceZoom;
        for (int i = 0; i < zoomScaledLayers.Length; i++)
        {
            if (zoomScaledLayers[i] == null)
                continue;

            Vector3 original = originalLayerScales[i];
            zoomScaledLayers[i].localScale = new Vector3(original.x * zoomRatio, original.y * zoomRatio, original.z);
        }

        if (background == null)
            return;

        Vector2 backgroundGoal = view.backgroundCenter;
        if (view.backgroundMode == ViewMode.FollowCamera)
            backgroundGoal += cameraGoal;
        else if (view.backgroundMode == ViewMode.FollowPlayer && player != null)
            backgroundGoal += (Vector2)player.position;

        SetCenter(background, Vector2.Lerp(backgroundStart, backgroundGoal, t));
    }

    private static void SetCenter(Transform target, Vector2 center)
    {
        target.position = new Vector3(center.x, center.y, target.position.z);
    }
}
