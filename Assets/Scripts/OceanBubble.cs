using UnityEngine;

/// <summary>Decoración compartida sin colliders ni asignaciones por fotograma.</summary>
public sealed class OceanBubble : MonoBehaviour
{
    public float floor = 2.2f;
    public float ceiling = 12.7f;
    public float speed = 0.8f;
    private float originX;
    private float phase;

    private void Awake() { originX = transform.position.x; phase = originX * 0.7f; }
    private void Update()
    {
        Vector3 position = transform.position;
        position.y += speed * Time.deltaTime;
        if (position.y > ceiling) position.y = floor;
        position.x = originX + Mathf.Sin(Time.time + phase) * 0.12f;
        transform.position = position;
    }
}
