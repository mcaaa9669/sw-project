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
        public BoxCollider2D leftExitBarrier;
        [Min(0.01f)] public float orthographicSize;
        public ViewMode backgroundMode;
        public Vector2 backgroundCenter;
        [Min(0f)] public float transitionDuration;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Transform background;
    [SerializeField] private Transform[] zoomScaledLayers = Array.Empty<Transform>();
    [SerializeField] private View[] views;

    private int currentView = -1;
    private Camera viewCamera;
    private float startingSize;
    private float sceneSize;
    private float referenceZoom;
    private Vector3[] originalLayerScales;
    private Vector2 cameraStart;
    private Vector2 backgroundStart;
    private float elapsed;

    public bool ControlsPlayer(PlayerController candidate) => candidate.transform == player;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        sceneSize = viewCamera.orthographicSize;
        referenceZoom = views != null && views.Length > 0 && views[0].orthographicSize > 0f
            ? views[0].orthographicSize : sceneSize;
        originalLayerScales = new Vector3[zoomScaledLayers.Length];
        for (int i = 0; i < zoomScaledLayers.Length; i++)
        {
            if (zoomScaledLayers[i] != null)
                originalLayerScales[i] = zoomScaledLayers[i].localScale;
        }
    }

    private void Start() => SetView(0);

    public void SetView(int index)
    {
        if (views == null || index < 0 || index >= views.Length || index == currentView)
            return;

        currentView = index;
        cameraStart = transform.position;
        startingSize = viewCamera.orthographicSize;
        if (background != null)
            backgroundStart = background.position;
        elapsed = 0f;
        ActivateLeftExitBarrier(views[index]);

        if (index == 0 || views[index].transitionDuration <= 0f)
            UpdateView(1f);
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

        elapsed += Time.deltaTime;
        float duration = views[currentView].transitionDuration;
        float t = duration > 0f ? Mathf.SmoothStep(0f, 1f, elapsed / duration) : 1f;
        UpdateView(t);
    }

    private void UpdateView(float t)
    {
        View view = views[currentView];
        Vector2 cameraGoal = view.cameraMode == ViewMode.FollowPlayer && player != null
            ? (Vector2)player.position + view.cameraCenter : view.cameraCenter;
        SetCenter(transform, Vector2.Lerp(cameraStart, cameraGoal, t));
        float size = view.orthographicSize > 0f ? view.orthographicSize : sceneSize;
        viewCamera.orthographicSize = Mathf.Lerp(startingSize, size, t);
        float zoomRatio = viewCamera.orthographicSize / referenceZoom;
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
