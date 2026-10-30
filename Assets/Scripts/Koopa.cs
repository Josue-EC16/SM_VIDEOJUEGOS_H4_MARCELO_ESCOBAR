using UnityEngine;

/// <summary>Alterna entre caminar, caparazón inmóvil, caparazón lanzado y derrota.</summary>
public class Koopa : MonoBehaviour
{
    public Sprite shellSprite;
    public float shellSpeed = 12f;

    private bool shelled;
    private bool pushed;
    private bool defeated;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!defeated && !shelled && collision.gameObject.CompareTag("Player") && collision.gameObject.TryGetComponent(out Player player))
        {
            if (player.starpower) {
                Hit();
            } else if (collision.HasContactNormal(Vector2.down)) {
                EnterShell();
            }  else {
                player.Hit();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (defeated) return;
        if (shelled && other.CompareTag("Player") && other.TryGetComponent(out Player player))
        {
            if (!pushed)
            {
                Vector2 direction = new(transform.position.x - other.transform.position.x, 0f);
                PushShell(direction);
            }
            else
            {
                if (player.starpower) {
                    Hit();
                } else {
                    player.Hit();
                }
            }
        }
        else if (!shelled && other.gameObject.layer == LayerMask.NameToLayer("Shell"))
        {
            Hit();
        }
    }

    private void EnterShell()
    {
        shelled = true;
        GameAudio.Instance?.Play(SoundEffect.Stomp);

        GetComponent<SpriteRenderer>().sprite = shellSprite;
        GetComponent<AnimatedSprite>().enabled = false;
        GetComponent<EntityMovement>().SetMovementAllowed(false);
    }

    private void PushShell(Vector2 direction)
    {
        pushed = true;
        GameAudio.Instance?.Play(SoundEffect.Kick);

        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        EntityMovement movement = GetComponent<EntityMovement>();
        movement.direction = direction.normalized;
        movement.speed = shellSpeed;
        movement.SetMovementAllowed(true);

        gameObject.layer = LayerMask.NameToLayer("Shell");
    }

    private void Hit()
    {
        if (defeated) return;
        defeated = true;
        GetComponent<AnimatedSprite>().enabled = false;
        GetComponent<DeathAnimation>().enabled = true;
        Destroy(gameObject, 3f);
    }

    private void OnBecameInvisible()
    {
        if (pushed) {
            Destroy(gameObject);
        }
    }

}
