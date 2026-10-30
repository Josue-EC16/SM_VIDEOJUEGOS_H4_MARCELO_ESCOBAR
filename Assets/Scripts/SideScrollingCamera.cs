using UnityEngine;

/// <summary>Avance horizontal con l?mites por regi?n y salto inmediato al usar tuber?as.</summary>
[RequireComponent(typeof(Camera))]
public class SideScrollingCamera : MonoBehaviour
{
    public Transform trackedObject;
    public float height = 6.5f;
    public float undergroundHeight = -9.5f;
    public float undergroundThreshold;
    public Vector2 horizontalBounds = new Vector2(0f, 225f);
    private Camera view;

    private void Awake() => view = GetComponent<Camera>();

    private void LateUpdate()
    {
        if (trackedObject == null) return;
        Vector3 position = transform.position;
        position.x = ClampX(Mathf.Max(position.x, trackedObject.position.x));
        transform.position = position;
    }

    private float ClampX(float x)
    {
        float halfWidth = view.orthographicSize * view.aspect;
        float left = horizontalBounds.x + halfWidth;
        float right = horizontalBounds.y - halfWidth;
        return right < left ? (horizontalBounds.x + horizontalBounds.y) * 0.5f : Mathf.Clamp(x, left, right);
    }

    public void SetRegion(bool underground, float y, Vector2 bounds, Color background, bool snap)
    {
        if (view == null) view = GetComponent<Camera>();
        horizontalBounds = bounds;
        Vector3 position = transform.position;
        position.y = y;
        if (snap && trackedObject != null) position.x = ClampX(trackedObject.position.x);
        transform.position = position;
        view.backgroundColor = background;
        GameAudio.Instance?.SetEnvironment(underground);
    }

    // Compatibilidad con la sala bonus de 1-1.
    public void SetUnderground(bool underground)
    {
        SetRegion(underground, underground ? undergroundHeight : height, horizontalBounds,
            underground ? Color.black : new Color(0.36f, 0.58f, 0.99f), true);
    }
}
