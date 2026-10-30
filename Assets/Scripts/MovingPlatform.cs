using UnityEngine;

/// <summary>Ascensor continuo; se recicla fuera del recorrido sin transportar al pasajero.</summary>
[DefaultExecutionOrder(-20)]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MovingPlatform : MonoBehaviour
{
    public float speed = 2f;
    public float bottom = -2f;
    public float top = 14f;
    public bool ascending;
    public Vector2 Displacement { get; private set; }
    private Rigidbody2D body;

    private void Awake() => body = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        Vector2 next = body.position + Vector2.up * ((ascending ? 1f : -1f) * speed * Time.fixedDeltaTime);
        if (next.y < bottom || next.y > top)
        {
            body.position = new Vector2(next.x, ascending ? bottom : top);
            Displacement = Vector2.zero;
        }
        else
        {
            Displacement = next - body.position;
            body.MovePosition(next);
        }
    }
}
