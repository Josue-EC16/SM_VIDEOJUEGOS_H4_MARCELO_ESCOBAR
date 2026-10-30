using UnityEngine;

/// <summary>Movimiento compartido. La visibilidad nunca desbloquea enemigos muertos o caparazones.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EntityMovement : MonoBehaviour
{
    public float speed = 1f;
    public Vector2 direction = Vector2.left;
    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool movementAllowed = true;

    private void Awake() { rb = GetComponent<Rigidbody2D>(); enabled = false; }
    private void OnBecameVisible() { if (movementAllowed) enabled = true; }
    private void OnBecameInvisible() => enabled = false;
    private void OnEnable() { if (rb != null) rb.WakeUp(); }
    private void OnDisable()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        velocity = Vector2.zero;
        rb.Sleep();
    }

    public void SetMovementAllowed(bool allowed)
    {
        movementAllowed = allowed;
        enabled = allowed;
    }

    private void FixedUpdate()
    {
        velocity.x = direction.x * speed;
        velocity.y += Physics2D.gravity.y * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        if (rb.Raycast(Vector2.down)) velocity.y = Mathf.Max(velocity.y, 0f);
        if (rb.Raycast(direction)) direction = -direction;
        transform.localEulerAngles = direction.x > 0f ? new Vector3(0f, 180f, 0f) : Vector3.zero;
    }
}
