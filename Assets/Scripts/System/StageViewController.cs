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
        [Min(0.01f)] public float orthographicSize;
        public ViewMode backgroundMode;
        public Vector2 backgroundCenter;
        [Min(0f)] public float transitionDuration;
    }

    [SerializeField] private Transform player;
    [SerializeField] private Transform background;
    [SerializeField] private View[] views;

    private int currentView = -1;
    private Camera viewCamera;
    private float startingSize;
    private float sceneSize;
    private Vector2 cameraStart;
    private Vector2 backgroundStart;
    private float elapsed;

    public bool ControlsPlayer(PlayerController candidate) => candidate.transform == player;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        sceneSize = viewCamera.orthographicSize;
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

        if (index == 0 || views[index].transitionDuration <= 0f)
            UpdateView(1f);
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
