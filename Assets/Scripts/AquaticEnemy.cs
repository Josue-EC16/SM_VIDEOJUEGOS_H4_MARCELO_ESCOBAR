using UnityEngine;

/// <summary>Pez de patrulla corta. En el agua se evita nadando; la estrella permite derrotarlo.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class AquaticEnemy : MonoBehaviour
{
    public Transform visual;
    public float speed = 1.1f;
    public float range = 2f;
    public float bob = 0.25f;
    private Rigidbody2D body;
    private Vector2 origin;
    private float phase;
    private float direction = 1f;
    private bool defeated;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        origin = body.position;
        phase = origin.x;
    }

    private void FixedUpdate()
    {
        Vector2 position = body.position;
        if (position.x >= origin.x + range) direction = -1f;
        else if (position.x <= origin.x - range) direction = 1f;
        position.x += speed * direction * Time.fixedDeltaTime;
        position.y = origin.y + Mathf.Sin(Time.time * 1.4f + phase) * bob;
        body.MovePosition(position);
        if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other) => Touch(other);
    private void OnTriggerStay2D(Collider2D other) => Touch(other);
    private void Touch(Collider2D other)
    {
        if (defeated || !other.TryGetComponent(out Player player) || player.dead || player.IsTransitioning) return;
        if (player.starpower)
        {
            defeated = true;
            GameAudio.Instance?.Play(SoundEffect.Kick);
            Destroy(gameObject);
        }
        else player.Hit();
    }
}
